namespace RhythmBase.Global.Utils.Tempo;

public enum ProcessingState
{
	LookingForOnsets = 0,
	ScanningIntervals = 1,
	RefiningIntervals = 2,
	SelectingBpmValues = 3,
	CalculatingOffsets = 4,
	MatchingOnsets = 5,
	Done = int.MaxValue,
}
public record struct TempoResult(double Bpm, double Offset, double Fitness);
public record struct Onset(int Pos, double Strength);
public record class TempoDetectionConfig
{
	/// <summary>BPM 搜索范围下限。</summary>
	public double MinBpm { get; set; } = 89.0;
	/// <summary>BPM 搜索范围上限。</summary>
	public double MaxBpm { get; set; } = 205.0;
	/// <summary>粗扫描步长（采样点数），越小越精细但越慢。</summary>
	public int IntervalDelta { get; set; } = 10;
	/// <summary>粗扫描下采样因子（2^n 倍降采样），影响粗扫描精度。</summary>
	public int IntervalDownsample { get; set; } = 3;
	/// <summary>粗扫描中保留候选的 fitness 最低门槛（相对于最大 fitness 的比例）。</summary>
	public double FitnessThresholdRatio { get; set; } = 0.4;
	/// <summary>BPM 去重的绝对容差（BPM），差值小于此值的候选被视为重复。</summary>
	public double DuplicateToleranceBpm { get; set; } = 0.5;
	/// <summary>BPM 去重的音程比容差（相对值），用于合并 3:2、4:3 等音程关系的候选。</summary>
	public double DuplicateRatioTolerance { get; set; } = 0.02;
	/// <summary>BPM 四舍五入到整数的直接容差，差值小于此值直接取整。</summary>
	public double RoundToleranceDirect { get; set; } = 0.01;
	/// <summary>BPM 四舍五入到整数的宽松容差，差值小于此值时在置信度允许下取整。</summary>
	public double RoundToleranceRelaxed { get; set; } = 0.05;
	/// <summary>宽松取整时要求的最低置信度比值（相对原值）。</summary>
	public double RoundConfidenceRatio { get; set; } = 0.99;
	/// <summary>top1/top2 fitness 比值低于此值时触发重评估。</summary>
	public double ReevaluateFitnessRatio { get; set; } = 1.05;
	/// <summary>fitness 归一化用的多项式阶数。</summary>
	public int PolyFitDegree { get; set; } = 3;
	/// <summary>onset 与节拍点匹配的容差（采样点数），onset 偏离节拍点小于此值视为匹配。</summary>
	public int OnsetMatchToleranceSamples { get; set; } = 100;
	/// <summary>计算 onset 强度时的局部平均窗口大小（采样点数）。</summary>
	public int OnsetStrengthWindowSamples { get; set; } = 100;
	/// <summary>onset 检测的 STFT 窗函数类型。</summary>
	public WindowType OnsetWindowType { get; set; } = WindowType.Hanning;
	/// <summary>BPM interval 置信度计算的窗函数类型。</summary>
	public WindowType IntervalWindowType { get; set; } = WindowType.Hamming;
	/// <summary>低通滤波截止频率（Hz），0 表示不启用。用于只关注低频内容（如重低音）。</summary>
	public double LowPassCutoffHz { get; set; } = 0;
	/// <summary>变速检测的滑动窗口大小（秒）。</summary>
	public double VariableTempoWindowSeconds { get; set; } = 4.0;
	/// <summary>变速检测窗口的重叠比例（0~1）。</summary>
	public double VariableTempoOverlap { get; set; } = 0.5;
	/// <summary>变速检测输出的最短片段时长（秒），低于此值的片段会被合并到相邻段。</summary>
	public double VariableTempoMinSegmentSeconds { get; set; } = 2.0;
	/// <summary>变速检测中相邻窗口合并的 BPM 容差，差值小于此值的相邻窗口会被合并为同一段。</summary>
	public double VariableTempoMergeToleranceBpm { get; set; } = 1.0;
	/// <summary>onset 检测使用的频谱差异方法。</summary>
	public SpecdescMethod Method { get; set; } = SpecdescMethod.HighFrequencyContent;
}
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
	public GapData(int bufferSize, int downsample, in Onset[] onsets, WindowType windowType = WindowType.Hamming)
	{
		this.onsets = onsets;
		this.downsample = downsample;
		this.windowSize = 2048 >> downsample;
		this.bufferSize = bufferSize;
		this.window = new double[this.windowSize];
		this.wrappedPos = new int[onsets.Length];
		this.wrappedOnsets = new double[bufferSize];
		Aubio.CreateWindowDouble(windowType, this.windowSize).CopyTo(this.window, 0);
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
	public IntervalTester(int samplerate, in Onset[] onsets, double minBpm, double maxBpm)
	{
		this.samplerate = samplerate;
		this.onsets = onsets;
		this.minInterval = (int)(60.0 / maxBpm * samplerate);
		this.maxInterval = (int)(60.0 / minBpm * samplerate);
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
	internal required int samplerate;
	internal int numThreads;
	internal byte? terminate;
	internal TempoDetectionConfig config = new();
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
	internal Onset[] Onsets = [];
	internal readonly List<TempoResult> result = [];
	internal bool[][] MatchedTempos = [];
	internal event Action<ProcessingState>? ProgressChanged;
};
internal static class FindTempo
{
	const int SimMaxColumns = 32;
	const int SimMaxPlayers = 16;
	const int SimDefaultBpm = 120;

	internal static void CreateHammingWindow(double[] buffer)
	{
		int N = buffer.Length;
		double t = 6.2831853071795864 / (N - 1);
		for (int n = 0; n < N; n++)
			buffer[n] = 0.54f - 0.46f * (float)Math.Cos(n * t);
	}
	internal static float[] ApplyLowPassFilter(float[] samples, int samplerate, double cutoffHz)
	{
		double omega = 2.0 * Math.PI * cutoffHz / samplerate;
		double alpha = Math.Sin(omega) / (2.0 * 0.7071067811865476); // Q = 0.707 (Butterworth)
		double cosOmega = Math.Cos(omega);

		double b0 = (1.0 - cosOmega) / 2.0;
		double b1 = 1.0 - cosOmega;
		double b2 = (1.0 - cosOmega) / 2.0;
		double a0 = 1.0 + alpha;
		double a1 = -2.0 * cosOmega;
		double a2 = 1.0 - alpha;

		Filter filter = Filter.CreateBiquad(b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0);
		float[] filtered = (float[])samples.Clone();
		float[] tmp = new float[filtered.Length];
		filter.DoFiltFilt(filtered, tmp);
		return filtered;
	}
	private static void NormalizeFitness(ref double fitness, in double[] coefs, double interval)
	{
		double
						x = interval,
						x2 = x * x,
						x3 = x2 * x;
		fitness -= coefs[0] + coefs[1] * x + coefs[2] * x2 + coefs[3] * x3;
	}
	private static double GapConfidence(in GapData gapdata, int gapPos, int interval)
	{
		int windowSize = gapdata.windowSize;
		int halfWindowSize = windowSize / 2;
		double[] window = gapdata.window;
		double area = 0.0;

		int beginOnset = gapPos - halfWindowSize;
		int endOnset = gapPos + halfWindowSize;

		if (beginOnset < 0)
		{
			int wrappedBegin = beginOnset + interval;
			for (int i = wrappedBegin; i < interval; ++i)
			{
				int windowIndex = i - wrappedBegin;
				area += gapdata.wrappedOnsets[i] * window[windowIndex];
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
				area += gapdata.wrappedOnsets[i] * window[windowIndex];
			}
			endOnset = interval;
		}
		for (int i = beginOnset; i < endOnset; ++i)
		{
			int windowIndex = i - beginOnset;
			area += gapdata.wrappedOnsets[i] * window[windowIndex];
		}
		return area;
	}
	private static double GetConfidenceForInterval(in GapData gapdata, int interval)
	{
		int downsample = gapdata.downsample;
		int numOnsets = gapdata.onsets.Length;
		Onset[] onsets = gapdata.onsets;

		Array.Fill(gapdata.wrappedOnsets, 0, 0, gapdata.bufferSize);

		int reduceInterval = interval >> downsample;
		for (int i = 0; i < numOnsets; ++i)
		{
			int pos = (onsets[i].Pos % interval) >> downsample;
			gapdata.wrappedPos[i] = pos;
			gapdata.wrappedOnsets[pos] += onsets[i].Strength;
		}

		double highestConfidence = 0.0;
		for (int i = 0; i < gapdata.onsets.Length; ++i)
		{
			int pos = gapdata.wrappedPos[i];
			double confidence = GapConfidence(gapdata, pos, reduceInterval);
			int offbeatPos = (pos + (reduceInterval >> 1)) % reduceInterval;
			confidence += GapConfidence(gapdata, offbeatPos, reduceInterval) * 0.5;

			if (confidence > highestConfidence)
				highestConfidence = confidence;
		}

		return highestConfidence;
	}
	private static double GetConfidenceForBPM(in GapData gapdata, IntervalTester test, double bpm)
	{
		int numOnsets = gapdata.onsets.Length;
		Onset[] onsets = gapdata.onsets;

		double intervalf = test.samplerate * (60.0 / bpm);
		int interval = (int)(intervalf + 0.5);
		if (interval > gapdata.bufferSize)
			interval = gapdata.bufferSize;

		Array.Fill(gapdata.wrappedOnsets, 0, 0, gapdata.bufferSize);

		for (int i = 0; i < numOnsets; ++i)
		{
			int pos = onsets[i].Pos % interval;
			gapdata.wrappedPos[i] = pos;
			gapdata.wrappedOnsets[pos] += onsets[i].Strength;
		}

		double highestConfidence = 0.0;
		for (int i = 0; i < gapdata.onsets.Length; ++i)
		{
			int pos = gapdata.wrappedPos[i];
			double confidence = GapConfidence(gapdata, pos, interval);
			int offbeatPos = (pos + (interval >> 1)) % interval;
			confidence += GapConfidence(gapdata, offbeatPos, interval) * 0.5;

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
	private static void FillCoarseIntervals(IntervalTester test, GapData gapdata, int numThreads, WindowType windowType, int intervalDelta)
	{
		int numCoarseIntervals = (test.numIntervals + intervalDelta - 1) / intervalDelta;
		if (numThreads > 1)
		{
			Parallel.For(0, numCoarseIntervals,
				new ParallelOptions() { MaxDegreeOfParallelism = numThreads },
				() => new GapData(gapdata.bufferSize, gapdata.downsample, gapdata.onsets, windowType),
				(i, _, localGapdata) =>
				{
					int index = i * intervalDelta;
					int interval = index + test.minInterval;
					test.fitness[index] = double.Max(0.001, GetConfidenceForInterval(localGapdata, interval));
					return localGapdata;
				},
				(localGapdata) => localGapdata.Dispose());
		}
		else
		{
			for (int i = 0; i < numCoarseIntervals; ++i)
			{
				int index = i * intervalDelta;
				int interval = index + test.minInterval;
				test.fitness[index] = double.Max(0.001, GetConfidenceForInterval(gapdata, interval));
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
				test.fitness[fitIndex] = GetConfidenceForInterval(gapdata, interval);
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
	private static void RemoveDuplicates(List<TempoResult> tempo, double tolerance, double ratioTolerance)
	{
		double[] ratios = [2.0, 3.0 / 2, 4.0 / 3, 5.0 / 4, 5.0 / 3, 6.0 / 5];
		for (int i = 0; i < tempo.Count; ++i)
		{
			double bpm = tempo[i].Bpm, doubled = bpm * 2.0, halved = bpm * 0.5;
			for (int j = tempo.Count - 1; j > i; --j)
			{
				double v = tempo[j].Bpm;
				bool remove = double.Min(double.Min(double.Abs(v - bpm), double.Abs(v - doubled)), double.Abs(v - halved)) < tolerance;
				if (!remove)
				{
					double ratio = v / bpm;
					foreach (double r in ratios)
					{
						if (double.Abs(ratio - r) < r * ratioTolerance || double.Abs(ratio - 1.0 / r) < ratioTolerance / r)
						{
							remove = true;
							break;
						}
					}
				}
				if (remove) tempo.RemoveAt(j);
			}
		}
	}
	private static void RoundBPMValues(IntervalTester test, GapData gapdata, List<TempoResult> tempo, TempoDetectionConfig config)
	{
		for (int i = 0; i < tempo.Count; i++)
		{
			TempoResult t = tempo[i];
			double roundBPM = double.Round(t.Bpm);
			double diff = double.Abs(roundBPM - t.Bpm);
			if (diff < config.RoundToleranceDirect)
			{
				t.Bpm = roundBPM;
			}
			else if (diff < config.RoundToleranceRelaxed)
			{
				double old = GetConfidenceForBPM(gapdata, test, t.Bpm);
				double cur = GetConfidenceForBPM(gapdata, test, roundBPM);
				if (cur > old * config.RoundConfidenceRatio) t.Bpm = roundBPM;
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
	internal static void CalculateBPM(ref SerializedTempo data, Onset[] onsets)
	{
		var tempo = data.result;
		var config = data.config;

		if (onsets.Length < 2)
		{
			tempo.Add(new TempoResult(SimDefaultBpm, 0.0, 1.0));
			return;
		}

		IntervalTester test = new(data.samplerate, onsets, config.MinBpm, config.MaxBpm);
		GapData gapdata = new(test.maxInterval, config.IntervalDownsample, onsets, config.IntervalWindowType);

		Array.Fill(test.fitness, 0, 0, test.numIntervals);
		FillCoarseIntervals(test, gapdata, data.numThreads, config.IntervalWindowType, config.IntervalDelta);
		int numCoarseIntervals = (test.numIntervals + config.IntervalDelta - 1) / config.IntervalDelta;
		if (data.terminate is not null) { return; }
		data.Progress = ProcessingState.RefiningIntervals;

		PolyFit(config.PolyFitDegree, test.coefs, test.fitness, numCoarseIntervals, test.minInterval);
		double maxFitness = 0.001;
		for (int i = 0; i < test.numIntervals; i += config.IntervalDelta)
		{
			NormalizeFitness(ref test.fitness[i], in test.coefs, i + test.minInterval);
			maxFitness = double.Max(maxFitness, test.fitness[i]);
		}

		double fitnessThreshold = maxFitness * config.FitnessThresholdRatio;
		for (int i = 0; i < test.numIntervals; i += config.IntervalDelta)
		{
			if (test.fitness[i] > fitnessThreshold)
			{
				(int x, int y) = FillIntervalRange(test, gapdata, i - config.IntervalDelta, i + config.IntervalDelta);
				int best = FindBestInterval(test, x, y);
				tempo.Add(new TempoResult(IntervalToBPM(in test, best), 0.0, test.fitness[best]));
			}
		}
		if (data.terminate is not null) { return; }
		data.Progress = ProcessingState.SelectingBpmValues;

		gapdata.Dispose();
		gapdata = new(test.maxInterval, 0, onsets, config.IntervalWindowType);

		tempo.Sort((a, b) => b.Fitness.CompareTo(a.Fitness));
		RemoveDuplicates(tempo, config.DuplicateToleranceBpm, config.DuplicateRatioTolerance);
		RoundBPMValues(test, gapdata, tempo, config);

		if (tempo.Count >= 2 && tempo[0].Fitness / tempo[1].Fitness < config.ReevaluateFitnessRatio)
		{
			for (int i = 0; i < tempo.Count; i++)
			{
				TempoResult t = tempo[i];
				t.Fitness = GetConfidenceForBPM(gapdata, test, t.Bpm);
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

		Array.Fill(gapdata.wrappedOnsets, 0, 0, gapdata.bufferSize);

		double intervalf = samplerate * 60.0 / bpm;
		int interval = (int)(intervalf + 0.5);
		Array.Fill(gapdata.wrappedOnsets, 0, 0, interval);
		for (int i = 0; i < numOnsets; ++i)
		{
			int pos = (int)(onsets[i].Pos % intervalf);
			gapdata.wrappedPos[i] = pos;
			gapdata.wrappedOnsets[pos] += 1.0;
		}

		double highestConfidence = 0.0;
		int offsetPos = 0;
		for (int i = 0; i < numOnsets; ++i)
		{
			int pos = gapdata.wrappedPos[i];
			double confidence = GapConfidence(gapdata, pos, interval);
			int offbeatPos = (pos + interval / 2) % interval;
			confidence += GapConfidence(gapdata, offbeatPos, interval) * 0.5;

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

		double secondsPerTickTime = 60.0 / bpm;
		double offbeat = offset + secondsPerTickTime * 0.5;
		if (offbeat > secondsPerTickTime) offbeat -= secondsPerTickTime;

		double end = numFrames;
		double interval = secondsPerTickTime * samplerate;
		double posA = offbeat * samplerate, sumA = 0.0;
		double posB = offbeat * samplerate, sumB = 0.0;
		for (; posA < end && posB < end; posA += interval, posB += interval)
		{
			sumA += slopes[(int)posA];
			sumB += slopes[(int)posB];
		}

		return sumA >= sumB ? offset : offbeat;
	}
	internal static void CalculateOffset(ref SerializedTempo data, Onset[] onsets)
	{
		var tempo = data.result;
		int samplerate = data.samplerate;
		var config = data.config;

		double maxInternal = 0;
		foreach (var t in tempo)
			maxInternal = double.Max(maxInternal, samplerate * 60.0 / t.Bpm);
		GapData gapdata = new((int)(maxInternal + 1.0), 1, onsets, config.IntervalWindowType);

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
	internal static void CalculateOnsets(ref SerializedTempo data, Onset[] onsets)
	{
		bool[][] fits = new bool[onsets.Length][];
		int tolerance = data.config.OnsetMatchToleranceSamples;
		for (int j = 0; j < onsets.Length; j++)
		{
			bool[] matchedtempos = new bool[data.result.Count];
			for (int i = 0; i < data.result.Count; i++)
			{
				Onset onset = onsets[j];
				TempoResult result = data.result[i];
				int pos = onset.Pos;
				double beatPeriod = 30.0 * data.samplerate / result.Bpm;
				double phase = (pos - result.Offset * data.samplerate) % beatPeriod;
				if (phase < 0) phase += beatPeriod;
				double diff = double.Min(phase, beatPeriod - phase);
				matchedtempos[i] = (diff < tolerance);
			}
			fits[j] = matchedtempos;
		}
		data.MatchedTempos = fits;
	}
}
public record struct TempoSegment(double StartSeconds, double EndSeconds, double Bpm, double Offset, double Fitness);
public class TempoDetector : IDisposable
{
	private readonly SerializedTempo data;
	private const double MinUniformSegmentSeconds = 6.0;
	private const double VariableTempoToleranceBpm = 1;
	public event Action<ProcessingState>? ProgressChanged;
	private const int MaxThreads = 8;
	public List<TempoResult> Results => data.result;
	public bool[][] Fits => data.MatchedTempos;
	public int Samplerate => data.samplerate;
	public Onset[] Onsets => data.Onsets;
	public TempoDetectionConfig Config { get; set; } = new();
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
	public TempoDetector(int firstFrame, int numFrames, int samplerate, float[] samples)
	{
		data = new SerializedTempo(numFrames)
		{
			numThreads = Math.Min(Environment.ProcessorCount, MaxThreads),
			samplerate = samplerate
		};
		data.ProgressChanged += (state) => { ProgressChanged?.Invoke(state); };

		Array.Copy(samples, firstFrame, data.samples, 0, numFrames);
	}
	public TempoDetector(int samplerate, float[][] samples)
	{
		data = new SerializedTempo(samples[0].Length)
		{
			numThreads = Math.Min(Environment.ProcessorCount, MaxThreads),
			samplerate = samplerate
		};
		data.ProgressChanged += (state) => { ProgressChanged?.Invoke(state); };

		for (int i = 0; i < samples.Length; ++i)
		{
			data.samples[i] = (samples[0][i] + samples[1][i]);
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
		Execute(ref data);
	}
	public List<Onset> DetectOnsets()
	{
		data.config = Config;
		List<Onset> onsets = [];
		FindOnsets.method = Config.Method;
		FindOnsets.windowType = Config.OnsetWindowType;
		float[] samples = Config.LowPassCutoffHz > 0
			? FindTempo.ApplyLowPassFilter(data.samples, data.samplerate, Config.LowPassCutoffHz)
			: data.samples;
		FindOnsets.Run(samples, data.samplerate, 1, onsets, Config);
		return onsets;
	}
	public List<TempoSegment> DetectVariableTempo()
	{
		int samplerate = data.samplerate;
		int totalSamples = data.samples.Length;
		double totalSeconds = (double)totalSamples / samplerate;

		int windowSamples = (int)(Config.VariableTempoWindowSeconds * samplerate);
		int hopSamples = (int)(windowSamples * (1.0 - Config.VariableTempoOverlap));
		if (hopSamples < 1) hopSamples = 1;

		var windows = new List<(double Start, double End, float[] Samples)>();
		for (int start = 0; start + windowSamples <= totalSamples; start += hopSamples)
		{
			float[] window = new float[windowSamples];
			Array.Copy(data.samples, start, window, 0, windowSamples);
			windows.Add(((double)start / samplerate, (double)(start + windowSamples) / samplerate, window));
		}
		if (windows.Count == 0)
		{
			float[] window = new float[totalSamples];
			Array.Copy(data.samples, 0, window, 0, totalSamples);
			windows.Add((0, totalSeconds, window));
		}

		var rawSegments = new List<TempoSegment>();
		for (int i = 0; i < windows.Count; i++)
		{
			var (start, end, samples) = windows[i];
			TempoDetector sub = new(samplerate, samples);
			sub.Config = Config;
			sub.Execute();
			if (sub.Results.Count > 0)
			{
				var best = sub.Results[0];
				rawSegments.Add(new(start, end, best.Bpm, best.Offset, best.Fitness));
			}
			sub.Dispose();
		}

		if (rawSegments.Count == 0) return rawSegments;

		var merged = new List<TempoSegment> { rawSegments[0] };
		for (int i = 1; i < rawSegments.Count; i++)
		{
			var last = merged[^1];
			var cur = rawSegments[i];
			if (double.Abs(cur.Bpm - last.Bpm) < Config.VariableTempoMergeToleranceBpm)
			{
				merged[^1] = last with
				{
					EndSeconds = cur.EndSeconds,
					Fitness = (last.Fitness + cur.Fitness) / 2
				};
			}
			else
			{
				merged.Add(cur);
			}
		}

		double minSeg = Config.VariableTempoMinSegmentSeconds;
		for (int i = merged.Count - 2; i >= 0; i--)
		{
			var cur = merged[i];
			if (cur.EndSeconds - cur.StartSeconds < minSeg)
			{
				var next = merged[i + 1];
				merged[i + 1] = next with { StartSeconds = cur.StartSeconds };
				merged.RemoveAt(i);
			}
		}
		if (merged.Count > 1)
		{
			var first = merged[0];
			if (first.EndSeconds - first.StartSeconds < minSeg)
			{
				merged[1] = merged[1] with { StartSeconds = first.StartSeconds };
				merged.RemoveAt(0);
			}
		}

		return merged;
	}
	public static void MergeSimilarSegments(List<TempoSegment> segments, double toleranceBpm)
	{
		if (segments.Count <= 1) return;

		segments.Sort((a, b) => a.StartSeconds.CompareTo(b.StartSeconds));

		for (int i = segments.Count - 2; i >= 0; i--)
		{
			var cur = segments[i];
			var next = segments[i + 1];
			if (double.Abs(cur.Bpm - next.Bpm) < toleranceBpm)
			{
				segments[i] = new TempoSegment(
					cur.StartSeconds,
					next.EndSeconds,
					(cur.Bpm + next.Bpm) / 2,
					cur.Fitness >= next.Fitness ? cur.Offset : next.Offset,
					double.Max(cur.Fitness, next.Fitness));
				segments.RemoveAt(i + 1);
			}
		}
	}

	private void Execute(ref SerializedTempo data)
	{
		data.config = Config;
		data.Progress = ProcessingState.LookingForOnsets;

		List<Onset> onsets = [];
		FindOnsets.method = Config.Method;
		FindOnsets.windowType = Config.OnsetWindowType;
		float[] onsetSamples = Config.LowPassCutoffHz > 0
			? FindTempo.ApplyLowPassFilter(data.samples, data.samplerate, Config.LowPassCutoffHz)
			: data.samples;
		FindOnsets.Run(onsetSamples, data.samplerate, 1, onsets, Config);
		Onset[] onsetsarray = onsets.ToArray();
		if (data.terminate is not null) { return; }
		data.Progress = ProcessingState.ScanningIntervals;

		int strengthWindow = Config.OnsetStrengthWindowSamples;
		for (int i = 0; i < onsetsarray.Length; ++i)
		{
			int a = int.Max(0, onsetsarray[i].Pos - strengthWindow);
			int b = int.Min(data.samples.Length, onsetsarray[i].Pos + strengthWindow);
			float v = 0.0f;
			for (int j = a; j < b; ++j)
			{
				v += float.Abs(data.samples[j]);
			}
			v /= float.Max(1, b - a);
			onsetsarray[i] = onsetsarray[i] with { Strength = v };
		}

		FindTempo.CalculateBPM(ref data, onsetsarray);
		if (data.terminate is not null) { return; }
		data.Progress = ProcessingState.CalculatingOffsets;

		FindTempo.CalculateOffset(ref data, onsetsarray);
		if (data.terminate is not null) { return; }
		data.Progress = ProcessingState.MatchingOnsets;

		FindTempo.CalculateOnsets(ref data, onsetsarray);
		if (data.terminate is not null) { return; }
		data.Onsets = onsetsarray;

		data.Progress = ProcessingState.Done;
	}
	public (double Seconds, TempoResult Result)[] ExecuteNonuniformSpeed()
	{
		var s = ExecuteNonuniformSpeedInternal(data, 0).OrderBy(i => i.Seconds).ToArray();
		return s;
	}
	//private (double Seconds, TempoResult Result, string depth)[] ExecuteNonuniformSpeedInternal(SerializedTempo data, string depth)
	//{
	//	int splitedLength = data.samples.Length / 2;
	//	SerializedTempo datar = new(data.samples.Length - splitedLength)
	//	{
	//		samplerate = data.samplerate,
	//		numThreads = data.numThreads
	//	};
	//	Array.Copy(data.samples, splitedLength, datar.samples, 0, datar.samples.Length);
	//	Execute(ref datar);
	//	if (double.Abs(datar.result[0].Bpm - data.result[0].Bpm) < VariableTempoToleranceBpm)
	//		return [(0, datar.result[0], depth + "-")];

	//	SerializedTempo datal = new(splitedLength)
	//	{
	//		samplerate = data.samplerate,
	//		numThreads = data.numThreads
	//	};
	//	Array.Copy(data.samples, 0, datal.samples, 0, datal.samples.Length);

	//	double middleSeconds = datal.samples.Length / (double)data.samplerate;
	//	var resultr = ExecuteNonuniformSpeedInternal(datar, depth + "R");
	//	Execute(ref datal);
	//	var resultl = ExecuteNonuniformSpeedInternal(datal, depth + "L");
	//	return [.. resultl, .. resultr.Select(i => (i.Seconds + middleSeconds, i.Result, depth))];
	//}
	private (double Seconds, TempoResult Result)[] ExecuteNonuniformSpeedInternal(SerializedTempo data, int depthlimit)
	{
		double diff;
		int depth = 0;
		int sampleLength = data.samples.Length;
		SerializedTempo datap = data, datasl, datasr;
		do
		{
			depth++;
			sampleLength = sampleLength / (1 << depth);
			datasl = new(sampleLength)
			{
				numThreads = data.numThreads,
				samplerate = data.samplerate,
				config = data.config
			};
			datasr = new(datap.samples.Length - sampleLength)
			{
				numThreads = data.numThreads,
				samplerate = data.samplerate,
				config = data.config
			};
			Array.Copy(datap.samples, 0, datasl.samples, 0, datasl.samples.Length);
			Array.Copy(datap.samples, sampleLength, datasr.samples, 0, datasr.samples.Length);
			Execute(ref datasl);
			Execute(ref datasr);

			diff = double.Abs(datap.result[0].Bpm - datasl.result[0].Bpm) + double.Abs(datasl.result[0].Bpm - datasr.result[0].Bpm);
			datap = datasl;
		}
		while (diff > VariableTempoToleranceBpm);

		throw new NotImplementedException();
	}
	public void Dispose()
	{
		GC.SuppressFinalize(this);
	}
}