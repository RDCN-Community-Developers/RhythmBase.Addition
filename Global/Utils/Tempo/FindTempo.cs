namespace RhythmBase.Global.Utils.Tempo;

public enum ProcessingState
{
	LookingForOnsets = 0,
	ScanningIntervals = 1,
	RefiningIntervals = 2,
	SelectingBpmValues = 3,
	CalculatingOffsets = 4,
	Done = 5
}
public record struct TempoResult(double Bpm, double Offset, double Fitness);
internal record struct Onset(int Pos, double Strength);
internal record class GapData : IDisposable
{
	internal readonly Onset[] onsets;
	internal int[] wrappedPos = [];
	internal double[] wrappedOnsets = [];
	internal double[] window = [];
	internal int
		bufferSize,
		windowSize,
		downsample;
	public GapData(int numThreads, int bufferSize, int downsample, in Onset[] onsets)
	{
		this.onsets = onsets;
		this.downsample = downsample;
		this.windowSize = 2048 >> downsample;
		this.bufferSize = bufferSize;
		this.window = new double[this.windowSize];
		this.wrappedPos = new int[onsets.Length * numThreads];
		this.wrappedOnsets = new double[bufferSize * numThreads];
		FindTempo.CreateHammingWindow(this.window);
	}
	public void Dispose()
	{
	}
}
internal record struct IntervalTester : IDisposable
{
	internal readonly int minInterval;
	internal readonly int maxInterval;
	internal readonly int numIntervals;
	internal readonly int samplerate;
	internal readonly int gapWindowSize;
	internal readonly Onset[] onsets;
	internal readonly double[] fitness;
	internal readonly double[] coefs = new double[4];
	public IntervalTester(int samplerate, in Onset[] onsets)
	{
		this.samplerate = samplerate;
		this.onsets = onsets;
		this.minInterval = (int)(60.0 / FindTempo.MaxBpm * samplerate);
		this.maxInterval = (int)(60.0 / FindTempo.MinBpm * samplerate);
		this.numIntervals = this.maxInterval - this.minInterval;

		fitness = new double[this.numIntervals];
	}
	public readonly void Dispose()
	{

	}
}
internal class SerializedTempo(int numFrames)
{
	internal readonly float[] samples = new float[numFrames];
	internal int samplerate;
	internal int numThreads;
	internal byte? terminate;
	internal ProcessingState Progress
	{
		get; set
		{
			lock (this)
			{
				field = value;
				ProgressChanged?.Invoke(value);
			}
		}
	}
	internal readonly List<TempoResult> result = [];
	internal event Action<ProcessingState>? ProgressChanged;
};
public static class FindTempo
{
	const int SimMaxColumns = 32;
	const int SimMaxPlayers = 16;
	const int SimDefaultBpm = 120;

	internal const double MinBpm = 89.0;
	internal const double MaxBpm = 205.0;
	private const int IntervalDelta = 10;
	private const int IntervalDownsample = 3;
	private const int MaxThreads = 1;

	internal static void CreateHammingWindow(double[] buffer)
	{
		int N = buffer.Length;
		double t = 6.2831853071795864 / (N - 1);
		for (int n = 0; n < N; n++)
			buffer[n] = 0.54f - 0.46f * (float)Math.Cos(n * t);
	}
	private static void NormalizeFitness(ref double fitness, in double[] coefs, double interval)
	{
		double
						x = interval,
						x2 = x * x,
						x3 = x2 * x;
		fitness -= coefs[0] + coefs[1] * x + coefs[2] * x2 + coefs[3] * x3;
	}
	private static double GapConfidence(in GapData gapdata, int threadId, int gapPos, int interval)
	{
		int windowSize = gapdata.windowSize;
		int halfWindowSize = windowSize / 2;
		double[] window = gapdata.window;
		int wrappedOnsetIndex = gapdata.bufferSize * threadId;
		double area = 0.0;

		int beginOnset = gapPos - halfWindowSize;
		int endOnset = gapPos + halfWindowSize;

		if (beginOnset < 0)
		{
			int wrappedBegin = beginOnset + interval;
			for (int i = wrappedBegin; i < interval; ++i)
			{
				int windowIndex = i - wrappedBegin;
				area += gapdata.wrappedOnsets[wrappedOnsetIndex + i] * window[windowIndex];
			}
			beginOnset = 0;
		}
		if (endOnset > interval)
		{
			int wrappedEnd = endOnset - interval;
			int indexOffset = windowSize - wrappedEnd;
			for (int i = 0; i < wrappedEnd; ++i)
			{
				int windowIndex = i + indexOffset;
				area += gapdata.wrappedOnsets[wrappedOnsetIndex + i] * window[windowIndex];
			}
			endOnset = interval;
		}
		for (int i = beginOnset; i < endOnset; ++i)
		{
			int windowIndex = i - beginOnset;
			area += gapdata.wrappedOnsets[wrappedOnsetIndex + i] * window[windowIndex];
		}
		return area;
	}
	private static double GetConfidenceForInterval(in GapData gapdata, int threadId, int interval)
	{
		int downsample = gapdata.downsample;
		int numOnsets = gapdata.onsets.Length;
		Onset[] onsets = gapdata.onsets;

		int wrappedPosIndex = gapdata.onsets.Length * threadId;
		int wrappedOnsetIndex = gapdata.bufferSize * threadId;
		Array.Fill(gapdata.wrappedOnsets, 0, wrappedPosIndex, gapdata.bufferSize);

		int reduceInterval = interval >> downsample;
		for (int i = 0; i < numOnsets; ++i)
		{
			int pos = (onsets[i].Pos % interval) >> downsample;
			gapdata.wrappedPos[wrappedPosIndex + i] = pos;
			gapdata.wrappedOnsets[wrappedOnsetIndex + pos] += onsets[i].Strength;
		}

		double highestConfidence = 0.0;
		for (int i = 0; i < gapdata.onsets.Length; ++i)
		{
			int pos = gapdata.wrappedPos[wrappedPosIndex + i];
			double confidence = GapConfidence(gapdata, threadId, pos, reduceInterval);
			int offbeatPos = (pos + (reduceInterval >> 1)) % reduceInterval;
			confidence += GapConfidence(gapdata, threadId, offbeatPos, reduceInterval) * 0.5;

			if (confidence > highestConfidence)
				highestConfidence = confidence;
		}

		return highestConfidence;
	}
	private static double GetConfidenceForBPM(in GapData gapdata, int threadId, IntervalTester test, double bpm)
	{
		int numOnsets = gapdata.onsets.Length;
		Onset[] onsets = gapdata.onsets;

		int wrappedPosIndex = gapdata.bufferSize * threadId;
		int wrappedOnsetIndex = gapdata.bufferSize * threadId;
		Array.Fill(gapdata.wrappedOnsets, 0, wrappedPosIndex, gapdata.bufferSize);

		double intervalf = test.samplerate * (60.0 / bpm);
		int interval = (int)(intervalf + 0.5);
		for (int i = 0; i < numOnsets; ++i)
		{
			int pos = onsets[i].Pos % interval;
			gapdata.wrappedPos[wrappedPosIndex + i] = pos;
			gapdata.wrappedOnsets[wrappedOnsetIndex + pos] += onsets[i].Strength;
		}

		double highestConfidence = 0.0;
		for (int i = 0; i < gapdata.onsets.Length; ++i)
		{
			int pos = gapdata.wrappedPos[wrappedPosIndex + i];
			double confidence = GapConfidence(gapdata, threadId, pos, interval);
			int offbeatPos = (pos + (interval >> 1)) % interval;
			confidence += GapConfidence(gapdata, threadId, offbeatPos, interval) * 0.5;

			if (confidence > highestConfidence)
				highestConfidence = confidence;
		}

		NormalizeFitness(ref highestConfidence, in test.coefs, intervalf);

		return highestConfidence;
	}
	private static double IntervalToBPM(in IntervalTester test, int i)
	{
		return (60.0 * test.samplerate) / (i + test.minInterval);
	}
	private static void FillCoarseIntervals(IntervalTester test, GapData gapdata, int numThreads)
	{
		int numCoarseIntervals = (test.numIntervals + IntervalDelta - 1) / IntervalDelta;
		if (numThreads > 1)
		{
			Parallel.For(0, numCoarseIntervals,
				new ParallelOptions() { MaxDegreeOfParallelism = 1}, (i) =>
				{
					int threadId = Environment.CurrentManagedThreadId % numThreads;
					int index = i * IntervalDelta;
					int interval = index + test.minInterval;
					test.fitness[index] = double.Max(0.001, GetConfidenceForInterval(gapdata, threadId, interval));
				});
		}
		else
		{
			for (int i = 0; i < numCoarseIntervals; ++i)
			{
				int index = i * IntervalDelta;
				int interval = index + test.minInterval;
				test.fitness[index] = double.Max(0.001, GetConfidenceForInterval(gapdata, 0, interval));
			}
		}
	}
	private static (int begin, int end) FillIntervalRange(IntervalTester test, GapData gapdata, int begin, int end)
	{
		begin = Math.Max(begin, 0);
		end = Math.Min(end, test.numIntervals);
		int fitIndex = begin;
		for (int i = begin, interval = test.minInterval + begin; i < end; ++i, ++interval, ++fitIndex)
		{
			if (test.fitness[fitIndex] == 0)
			{
				test.fitness[fitIndex] = GetConfidenceForInterval(gapdata, 0, interval);
				NormalizeFitness(ref test.fitness[fitIndex], in test.coefs, interval);
				test.fitness[fitIndex] = double.Max(0.1, test.fitness[fitIndex]);
			}
		}
		return (begin, end);
	}
	private static int FindBestInterval(in IntervalTester test, int begin, int end)
	{
		int bestInterval = begin + test.minInterval;
		double highestFitness = 0.0;
		for (int i = begin; i < end; ++i)
		{
			double fitness = test.fitness[i];
			if (fitness > highestFitness)
			{
				highestFitness = fitness;
				bestInterval = i;
			}
		}
		return bestInterval;
	}
	private static void RemoveDuplicates(List<TempoResult> tempo)
	{
		for (int i = 0; i < tempo.Count; ++i)
		{
			double bpm = tempo[i].Bpm, doubled = bpm * 2.0, halved = bpm * 0.5;
			for (int j = tempo.Count - 1; j > i; --j)
			{
				double v = tempo[j].Bpm;
				if (double.Min(double.Min(double.Abs(v - bpm), double.Abs(v - doubled)), double.Abs(v - halved)) < 0.1)
					tempo.RemoveAt(j);
			}
		}
	}
	private static void RoundBPMValues(IntervalTester test, GapData gapdata, List<TempoResult> tempo)
	{
		for (int i = 0; i < tempo.Count; i++)
		{
			TempoResult t = tempo[i];
			double roundBPM = double.Round(t.Bpm);
			double diff = double.Abs(roundBPM - t.Bpm);
			if (diff < 0.01)
			{
				t.Bpm = roundBPM;
			}
			else if (diff < 0.05)
			{
				double old = GetConfidenceForBPM(gapdata, 0, test, t.Bpm);
				double cur = GetConfidenceForBPM(gapdata, 0, test, roundBPM);
				if (cur > old * 0.99) t.Bpm = roundBPM;
			}
			tempo[i] = t;
		}
	}
	private static void PolyFit(int degree, double[] outCoefs, double[] inValues, int numNonZeroValues, int offsetX)
	{
		++degree;

		int numValues = numNonZeroValues;
		Matrix<double> xm = new(numValues, degree);
		Matrix<double> ym = new(numValues, 1);

		for (int row = 0, i = 0; row < numValues; ++row, ++i)
		{
			while (inValues[i] == 0) ++i;
			ym[row, 0] = inValues[i];
		}
		for (int row = 0, i = 0; row < numValues; ++row, ++i)
		{
			while (inValues[i] == 0) ++i;
			double val = 1.0, x = offsetX + i;
			for (int col = 0; col < degree; col++)
			{
				xm[row, col] = val;
				val *= x;
			}
		}
		Matrix<double> xtm = xm.Transpose();
		Matrix<double> xtxm = xtm * xm;
		Matrix<double> xtym = xtm * ym;
		Givens givens = new();
		givens.Decompose(xtxm);
		Matrix<double> coeff = givens.Solve(xtym);
		for (int i = 0; i < degree; ++i)
			outCoefs[i] = coeff[0, i];
	}
	private static void CalculateBPM(ref SerializedTempo data, Onset[] onsets)
	{
		var tempo = data.result;

		if (onsets.Length < 2)
		{
			tempo.Add(new TempoResult(SimDefaultBpm, 0.0, 1.0));
			return;
		}

		IntervalTester test = new(data.samplerate, onsets);
		GapData gapdata = new(data.numThreads, test.maxInterval, IntervalDownsample, onsets);

		Array.Fill(test.fitness, 0, 0, test.numIntervals);
		FillCoarseIntervals(test, gapdata, data.numThreads);
		int numCoarseIntervals = (test.numIntervals + IntervalDelta - 1) / IntervalDelta;
		if (data.terminate is not null) { return; }
		data.Progress = ProcessingState.RefiningIntervals;

		PolyFit(3, test.coefs, test.fitness, numCoarseIntervals, test.minInterval);
		double maxFitness = 0.001;
		for (int i = 0; i < test.numIntervals; i += IntervalDelta)
		{
			NormalizeFitness(ref test.fitness[i], in test.coefs, i + test.minInterval);
			maxFitness = double.Max(maxFitness, test.fitness[i]);
		}

		double fitnessThreshold = maxFitness * 0.4;
		for (int i = 0; i < test.numIntervals; i += IntervalDelta)
		{
			if (test.fitness[i] > fitnessThreshold)
			{
				(int x, int y) = FillIntervalRange(test, gapdata, i - IntervalDelta, i + IntervalDelta);
				int best = FindBestInterval(test, x, y);
				tempo.Add(new TempoResult(IntervalToBPM(in test, best), 0.0, test.fitness[best]));
			}
		}
		if (data.terminate is not null) { return; }
		data.Progress = ProcessingState.SelectingBpmValues;

		gapdata.Dispose();
		gapdata = new(data.numThreads, test.maxInterval, 0, onsets);

		tempo.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
		RemoveDuplicates(tempo);
		RoundBPMValues(test, gapdata, tempo);

		if (tempo.Count >= 2 && tempo[0].Fitness / tempo[1].Fitness < 1.05)
		{
			for (int i = 0; i < tempo.Count; i++)
			{
				TempoResult t = tempo[i];
				t.Fitness = GetConfidenceForBPM(gapdata, 0, test, t.Bpm);
				tempo[i] = t;
			}
			tempo.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
		}

		// Keep only top 3 tempos
		//if (tempo.Count > 3) tempo.RemoveRange(3, tempo.Count - 3);

		gapdata.Dispose();
	}
	private static void ComputeSlopes(in float[] samples, double[] o, int samplerate)
	{
		Array.Fill(o, 0, 0, o.Length);

		int wh = samplerate / 20;
		if (samples.Length < wh * 2) return;

		double sumL = 0, sumR = 0;
		for (int i = 0, j = wh; i < wh; ++i, ++j)
		{
			sumL += double.Abs(samples[i]);
			sumR += double.Abs(samples[j]);
		}

		double scalar = 1.0 / wh;
		for (int i = wh, end = samples.Length - wh; i < end; ++i)
		{
			o[i] = double.Max(0, (sumR - sumL) * scalar);

			double cur = double.Abs(samples[i]);
			sumL -= double.Abs(samples[i - wh]);
			sumL += cur;
			sumR -= cur;
			sumR += double.Abs(samples[i + wh]);
		}
	}
	private static double GetBaseOffsetValue(in GapData gapdata, int samplerate, double bpm)
	{
		int numOnsets = gapdata.onsets.Length;
		Onset[] onsets = gapdata.onsets;

		int wrappedPosIndex = 0;
		int wrappedOnsetsIndex = 0;
		Array.Fill(gapdata.wrappedOnsets, wrappedOnsetsIndex, 0, gapdata.bufferSize);

		double intervalf = samplerate * 60.0 / bpm;
		int interval = (int)(intervalf + 0.5);
		Array.Fill(gapdata.wrappedOnsets, wrappedOnsetsIndex, 0, interval);
		for (int i = 0; i < numOnsets; ++i)
		{
			int pos = (int)(onsets[i].Pos % intervalf);
			gapdata.wrappedPos[wrappedPosIndex + i] = pos;
			gapdata.wrappedOnsets[wrappedPosIndex + pos] += 1.0;
		}

		double highestConfidence = 0.0;
		int offsetPos = 0;
		for (int i = 0; i < numOnsets; ++i)
		{
			int pos = gapdata.wrappedPos[wrappedPosIndex + i];
			double confidence = GapConfidence(gapdata, 0, pos, interval);
			int offbeatPos = (pos + interval / 2) % interval;
			confidence += GapConfidence(gapdata, 0, offbeatPos, interval) * 0.5;

			if (confidence > highestConfidence)
			{
				highestConfidence = confidence;
				offsetPos = pos;
			}
		}

		return offsetPos / (double)samplerate;
	}
	private static double AdjustForOffbeats(ref SerializedTempo data, double offset, double bpm)
	{
		int samplerate = data.samplerate;
		int numFrames = data.samples.Length;

		double[] slopes = new double[numFrames];
		ComputeSlopes(data.samples, slopes, samplerate);

		double secondsPerBeat = 60.0 / bpm;
		double offbeat = offset + secondsPerBeat * 0.5;
		if (offbeat > secondsPerBeat) offbeat -= secondsPerBeat;

		double end = numFrames;
		double interval = secondsPerBeat * samplerate;
		double posA = offbeat * samplerate, sumA = 0.0;
		double posB = offbeat * samplerate, sumB = 0.0;
		for (; posA < end && posB < end; posA += interval, posB += interval)
		{
			sumA += slopes[(int)posA];
			sumB += slopes[(int)posB];
		}

		return sumA >= sumB ? offset : offbeat;
	}
	private static void CalculateOffset(ref SerializedTempo data, Onset[] onsets)
	{
		var tempo = data.result;
		int samplerate = data.samplerate;

		double maxInternal = 0;
		foreach (var t in tempo)
			maxInternal = double.Max(maxInternal, samplerate * 60.0 / t.Bpm);
		GapData gapdata = new(1, (int)(maxInternal + 1.0), 1, onsets);

		for (int i = 0; i < tempo.Count; i++)
		{
			TempoResult t = tempo[i];
			t.Offset = GetBaseOffsetValue(gapdata, samplerate, t.Bpm);
			tempo[i] = t;
		}

		for (int i = 0; i < tempo.Count; i++)
		{
			TempoResult t = tempo[i];
			t.Offset = AdjustForOffbeats(ref data, t.Offset, t.Bpm);
			tempo[i] = t;
		}
	}
	public class TempoDetector : IDisposable
	{
		private readonly SerializedTempo data;
		public event Action<ProcessingState>? ProgressChanged;
		public List<TempoResult> Results => data.result;
		public SpecdescMethod Method { get; set; } = SpecdescMethod.HighFrequencyContent;
		public int ThreadCount
		{
			get => data.numThreads;
			set => data.numThreads = Math.Min(value, MaxThreads);
		}
		public TempoDetector(int firstFrame, int numFrames, int samplerate, float[][] samples)
		{
			data = new SerializedTempo(numFrames)
			{
				numThreads = Math.Min(Environment.ProcessorCount, MaxThreads),
				samplerate = samplerate
			};
			data.ProgressChanged += (state) => { ProgressChanged?.Invoke(state); };

			for (int i = 0; i < numFrames; ++i)
			{
				data.samples[i] = (samples[0][firstFrame + i] + samples[1][firstFrame + i]);
			}
		}
		public TempoDetector(int samplerate, float[] samples)
		{
			data = new SerializedTempo(samples.Length)
			{
				numThreads = Math.Min(Environment.ProcessorCount, MaxThreads),
				samplerate = samplerate
			};
			data.ProgressChanged += (state) => { ProgressChanged?.Invoke(state); };

			Array.Copy(samples, data.samples, samples.Length);
		}
		public void Execute()
		{
			SerializedTempo data = this.data;
			data.Progress = ProcessingState.LookingForOnsets;

			List<Onset> onsets = [];
			FindOnsets.method = Method;
			FindOnsets.Run(data.samples, data.samplerate, 1, onsets);
			if (data.terminate is not null) { return; }
			data.Progress = ProcessingState.ScanningIntervals;

			for (int i = 0; i < int.Min(onsets.Count, 100); ++i)
			{
				int a = int.Max(0, onsets[i].Pos - 100);
				int b = int.Min(data.samples.Length, onsets[i].Pos + 100);
				float v = 0.0f;
				for (int j = a; j < b; ++j)
				{
					v += float.Abs(data.samples[j]);
				}
				v /= float.Max(1, b - a);
				onsets[i] = onsets[i] with { Strength = v };
			}

			CalculateBPM(ref data, [.. onsets]);
			if (data.terminate is not null) { return; }
			data.Progress = ProcessingState.CalculatingOffsets;

			CalculateOffset(ref data, [.. onsets]);
			if (data.terminate is not null) { return; }
			data.Progress = ProcessingState.Done;
		}
		public void Dispose()
		{
			GC.SuppressFinalize(this);
		}
	}
}
