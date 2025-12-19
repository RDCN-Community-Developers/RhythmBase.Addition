using System.Collections.Concurrent;

namespace RhythmBase.Global.Utils.Tempo;

public enum SpecdescMethod
{
	Energy,
	SpectralDifference,
	HighFrequencyContent,
	ComplexDomain,
	Phase,
	KullbackLeibler,
	ModifiedKullbackLeibler,
	SpectralFlux,
	Default = HighFrequencyContent,
}

internal class ComplexVector
{
	internal readonly float[] norm;
	internal readonly float[] phas;
	internal int Length;
	public ComplexVector(int n)
	{
		Length = n / 2 + 1;
		norm = new float[Length];
		phas = new float[Length];
	}
}

internal class Scale
{
	private float ilow;
	private float ihigh;
	private float olow;
	private float ohigh;
	private float scaler;
	private readonly float irange;
	public Scale(float ilow, float ihigh, float olow, float ohigh)
	{
		SetLimits(ilow, ihigh, olow, ohigh);
	}
	internal void SetLimits(float ilow, float ihigh, float olow, float ohigh)
	{
		float inputrange = ihigh - ilow;
		float outputrange = ohigh - olow;
		this.ilow = ilow;
		this.ihigh = ihigh;
		this.olow = olow;
		this.ohigh = ohigh;
		if (inputrange == 0)
			this.scaler = 0;
		else
		{
			scaler = outputrange / inputrange;
			if (inputrange < 0)
				inputrange *= -1f;
		}
	}
	internal void Do(float[] input)
	{
		for (int j = 0; j < input.Length; ++j)
		{
			input[j] -= ilow;
			input[j] *= scaler;
			input[j] += olow;
		}
	}
}

internal class Hist
{
	private readonly float[] hist;
	private readonly uint nelems;
	private readonly float[] cent;
	private readonly Scale scaler;
	public Hist(float flow, float fhigh, uint nelems)
	{
		float step = (fhigh - flow) / nelems;
		float accum = step;
		this.nelems = nelems;
		this.hist = new float[nelems];
		this.cent = new float[nelems];

		scaler = new Scale(flow, fhigh, 0f, nelems);
		cent[0] = flow + step / 2f;
		for (int i = 1; i < this.nelems; ++i, accum += step)
			this.cent[i] = this.cent[0] + accum;
	}
	internal void DynNotnull(float[] input)
	{
		int tmp;
		float ilow = input.Min();
		float ihigh = input.Max();
		float step = (ihigh - ilow) / nelems;

		scaler.SetLimits(ilow, ihigh, 0f, nelems);

		cent[0] = ilow + step / 2f;
		for (int i = 1; i < this.nelems; ++i)
			this.cent[i] = this.cent[0] + i * step;

		scaler.Do(input);

		Array.Clear(hist, 0, hist.Length);
		for (int i = 0; i < input.Length; ++i)
		{
			if (input[i] != 0)
			{
				tmp = (int)float.Floor(input[i]);
				if ((tmp >= 0) && (tmp < nelems))
					hist[tmp] += 1;
			}
		}
	}
	internal void Weight()
	{
		for (int j = 0; j < nelems; ++j)
			hist[j] *= cent[j];
	}
	internal float Average() => hist.Average();
}
internal class Fft
{
	private readonly int winsize;
	private readonly int fftsize;
	private readonly float[] i, o;
	private readonly float[] w;
	private readonly int[] ip;
	private readonly float[] compspec;
	public Fft(int winsize)
	{
		this.winsize = winsize;
		this.fftsize = winsize / 2 + 1;
		this.compspec = new float[winsize];
		this.i = new float[winsize];
		this.o = new float[winsize];
		this.ip = new int[fftsize];
		this.w = new float[fftsize];
		this.ip[0] = 0;
	}
	private void DoComplex(float[] input, float[] compspec)
	{
		Array.Copy(input, i, winsize);
		FFT.rdft(winsize, 1, i, ip, w);
		compspec[0] = i[0];
		compspec[winsize / 2] = i[1];
		for (int i = 1; i < fftsize - 1; ++i)
		{
			compspec[i] = this.i[2 * i];
			compspec[winsize - i] = -this.i[2 * i + 1];
		}
	}
	private static void GetPhas(float[] compspec, ComplexVector spectrum)
	{
		if (compspec[0] < 0)
			spectrum.phas[0] = float.Pi;
		else
			spectrum.phas[0] = 0;

		for (int i = 1; i < spectrum.Length - 1; ++i)
			spectrum.phas[i] = float.Atan2(compspec[spectrum.Length - i], compspec[i]);

		if (compspec[compspec.Length / 2] < 0)
			spectrum.phas[spectrum.Length - 1] = float.Pi;
		else
			spectrum.phas[spectrum.Length - 1] = 0;
	}
	private static void GetNorm(float[] compspec, ComplexVector spectrum)
	{
		spectrum.norm[0] = float.Abs(compspec[0]);
		for (int i = 1; i < spectrum.Length - 1; ++i)
			spectrum.norm[i] = float.Sqrt(compspec[i] * compspec[i] + compspec[^i] * compspec[^i]);
		spectrum.norm[spectrum.Length - 1] = float.Abs(compspec[compspec.Length / 2]);
	}
	internal void Do(float[] input, ComplexVector spectrum)
	{
		DoComplex(input, compspec);
		GetPhas(compspec, spectrum);
		GetNorm(compspec, spectrum);
	}
}

internal class Filter
{
	private readonly int order;
	private readonly int samplerate;
	private readonly double[] a;
	private readonly double[] b;
	private readonly double[] y;
	private readonly double[] x;
	public double[] Feedback => a;
	public double[] Feedforward => b;
	public int Order => order;
	public Filter(int order)
	{
		this.order = order;
		this.a = new double[order];
		this.b = new double[order];
		this.x = new double[order];
		this.y = new double[order];
		this.samplerate = 0;
		this.a[1] = 1;
	}
	private void SetBiquad(double b0, double b1, double b2, double a1, double a2)
	{
		if (Order != 3) throw new InvalidOperationException("Filter order must be 3 for biquad.");
		b[0] = b0;
		b[1] = b1;
		b[2] = b2;
		a[0] = 1;
		a[1] = a1;
		a[2] = a2;
	}
	public static Filter CreateBiquad(double b0, double b1, double b2, double a1, double a2)
	{
		Filter filter = new(3);
		filter.SetBiquad(b0, b1, b2, a1, a2);
		return filter;
	}
	private static float KillDenormal(float value)
	{
		return float.Abs(value) < 2e-42 ? 0 : value;
	}
	private void Do(float[] i)
	{
		for (int j = 0; j < i.Length; ++j)
		{
			x[0] = KillDenormal(i[j]);
			y[0] = b[0] * x[0];
			for (int l = 1; l < order; ++l)
			{
				y[0] += b[l] * x[l] - a[l] * y[l];
			}
			i[j] = (float)y[0];
			for (int l = order - 1; l > 0; --l)
			{
				x[l] = x[l - 1];
				y[l] = y[l - 1];
			}
		}
	}
	private void DoReset()
	{
		Array.Clear(x, 0, x.Length);
		Array.Clear(y, 0, y.Length);
	}
	internal void DoFiltFilt(float[] i, float[] tmp)
	{
		Do(i);
		DoReset();
		for (int j = 0; j < i.Length; ++j)
			tmp[i.Length - j - 1] = i[j];
		Do(tmp);
		DoReset();
		for (int j = 0; j < i.Length; ++j)
			i[j] = tmp[i.Length - j - 1];
	}
}

internal class Specdesc
{
	private readonly SpecdescMethod type;
	private readonly Action<ComplexVector, float[]> Func;
	private readonly float threshold;
	private readonly float[] oldmag;
	private readonly float[] dev1;
	private readonly float[] theta1;
	private readonly float[] theta2;
	private readonly Hist hist;
	public Specdesc(SpecdescMethod type, int size)
	{
		int rsize = size / 2 + 1;
		this.type = type;
		switch (type)
		{
			case SpecdescMethod.Energy:
				Func = Energy;
				break;
			case SpecdescMethod.HighFrequencyContent:
				Func = HighFrequencyContent;
				break;
			case SpecdescMethod.ComplexDomain:
				oldmag = new float[rsize];
				dev1 = new float[rsize];
				theta1 = new float[rsize];
				theta2 = new float[rsize];
				Func = Complex;
				break;
			case SpecdescMethod.Phase:
				dev1 = new float[rsize];
				theta1 = new float[rsize];
				theta2 = new float[rsize];
				hist = new Hist(0, float.Pi, 40);
				threshold = 0.1f;
				Func = Phase;
				break;
			case SpecdescMethod.SpectralDifference:
				oldmag = new float[rsize];
				dev1 = new float[rsize];
				hist = new Hist(0, float.Pi, 40);
				threshold = 0.1f;
				Func = SpectralDifference;
				break;
			case SpecdescMethod.KullbackLeibler:
				oldmag = new float[rsize];
				Func = KullbackLeibler;
				break;
			case SpecdescMethod.ModifiedKullbackLeibler:
				oldmag = new float[rsize];
				Func = ModifiedKullbackLeibler;
				break;
			case SpecdescMethod.SpectralFlux:
				oldmag = new float[rsize];
				Func = SpectralFlux;
				break;
			default:
				goto case SpecdescMethod.HighFrequencyContent;
		}
	}
	internal static void Energy(ComplexVector fftgrain, float[] onset)
	{
		onset[0] = 0;
		for (int j = 0; j < fftgrain.Length; ++j)
			onset[0] += fftgrain.norm[j] * fftgrain.norm[j];
	}
	internal static void HighFrequencyContent(ComplexVector fftgrain, float[] onset)
	{
		onset[0] = 0;
		for (int j = 0; j < fftgrain.Length; ++j)
			onset[0] += (j + 1) * fftgrain.norm[j];
	}
	internal void Complex(ComplexVector fftgrain, float[] onset)
	{
		int nbins = fftgrain.Length;
		onset[0] = 0;
		for (int j = 0; j < nbins; ++j)
		{
			dev1[j] = 2 * theta1[j] - theta2[j];
			onset[0] +=
					(float)double.Sqrt(double.Abs((oldmag[j] * oldmag[j]) + (fftgrain.norm[j] * fftgrain.norm[j])
					- 2.0 * oldmag[j] * fftgrain.norm[j]
					* double.Cos(dev1[j] - fftgrain.phas[j])));
			theta2[j] = theta1[j];
			theta1[j] = fftgrain.phas[j];
			oldmag[j] = fftgrain.norm[j];
		}
	}
	internal void Phase(ComplexVector fftgrain, float[] onset)
	{
		int nbins = fftgrain.Length;
		onset[0] = 0;
		dev1[0] = 0;
		for (int j = 0; j < nbins; ++j)
		{
			dev1[j] =
					Aubio.UnwrapToPi(fftgrain.phas[j] - 2 * theta1[j] + theta2[j]);
			if (threshold < fftgrain.norm[j])
				dev1[j] = float.Abs(dev1[j]);
			else
				dev1[j] = 0;
			theta2[j] = theta1[j];
			theta1[j] = fftgrain.phas[j];
		}
		hist.DynNotnull(dev1);
		hist.Weight();
		onset[0] = hist.Average();
	}
	internal void SpectralDifference(ComplexVector fftgrain, float[] onset)
	{
		int nbins = fftgrain.Length;
		onset[0] = 0;
		dev1[0] = 0;
		for (int j = 0; j < nbins; ++j)
		{
			dev1[j] = (float)double.Sqrt(
					double.Abs(fftgrain.norm[j] * fftgrain.norm[j]
					- oldmag[j] * oldmag[j]));
			if (threshold < fftgrain.norm[j])
				dev1[j] = float.Abs(dev1[j]);
			else
				dev1[j] = 0;
			oldmag[j] = fftgrain.norm[j];
		}
		hist.DynNotnull(dev1);
		hist.Weight();
		onset[0] = hist.Average();
	}
	internal void KullbackLeibler(ComplexVector fftgrain, float[] onset)
	{
		onset[0] = 0;
		for (int j = 0; j < fftgrain.Length; ++j)
		{
			onset[0] += (float)(fftgrain.norm[j]
					* double.Log(1.0 + fftgrain.norm[j] / (oldmag[j] + 1e-1)));
		}
		if (float.IsNaN(onset[0])) onset[0] = 0;
	}
	internal void ModifiedKullbackLeibler(ComplexVector fftgrain, float[] onset)
	{
		onset[0] = 0;
		for (int j = 0; j < fftgrain.Length; ++j)
		{
			onset[0] += (float)(fftgrain.norm[j] * double.Log(1.0 + fftgrain.norm[j] / (oldmag[j] + 1e-1)));
			oldmag[j] = fftgrain.norm[j];
		}
		if (float.IsNaN(onset[0])) onset[0] = 0;
	}
	internal void SpectralFlux(ComplexVector fftgrain, float[] onset)
	{
		onset[0] = 0;
		for (int j = 0; j < fftgrain.Length; ++j)
		{
			if (fftgrain.norm[j] > oldmag[j])
				onset[0] += fftgrain.norm[j] - oldmag[j];
			oldmag[j] = fftgrain.norm[j];
		}
	}
	public void Do(ComplexVector fftgraion, float[] onset)
	{
		Func(fftgraion, onset);
	}
}

internal delegate float ThresholdFunc(float[] input);
internal delegate bool PickerFunc(float[] input, int pos);

internal class PeakPicker
{
	private float threshold;
	private readonly int win_post;
	private readonly int win_pre;
	private readonly ThresholdFunc threshfunc;
	private readonly PickerFunc pickfunc;
	private readonly Filter biquad;
	private readonly float[] onset_keep;
	private readonly float[] onset_proc;
	private readonly float[] onset_peek;
	private readonly float[] thresholded;
	private readonly float[] scratch;
	public float Threshold { get => threshold; set => threshold = value; }
	public PeakPicker()
	{
		threshold = 0.1f;
		win_post = 5;
		win_pre = 1;

		threshfunc = Aubio.Median;
		pickfunc = Aubio.PeakPick;

		scratch = new float[win_post + win_pre + 1];
		onset_keep = new float[win_post + win_pre + 1];
		onset_proc = new float[win_post + win_pre + 1];
		onset_peek = new float[3];
		thresholded = new float[1];

		biquad = Filter.CreateBiquad(0.15998789, 0.31997577, 0.15998789,
		-0.59488894, 0.23484048);
	}
	public void Do(float[] input, float[] output)
	{
		float mean, median = 0;
		int length = win_post + win_pre + 1;
		for (int j = 0; j < length - 1; ++j)
		{
			onset_keep[j] = onset_keep[j + 1];
			onset_proc[j] = onset_keep[j];
		}
		onset_keep[length - 1] = input[0];
		onset_proc[length - 1] = input[0];

		biquad.DoFiltFilt(onset_proc, scratch);

		mean = onset_proc.Average();
		Array.Copy(onset_proc, scratch, length);
		median = threshfunc(scratch);

		for (int j = 0; j < 3 - 1; j++)
			onset_peek[j] = onset_peek[j + 1];

		thresholded[0] = onset_proc[win_post] - median - mean * threshold;
		onset_peek[2] = thresholded[0];
		output[0] = pickfunc(onset_peek, 1) ? Aubio.QuadraticPeakPos(onset_peek, 1) : 0f;
	}
}
internal class Pvoc
{
	private readonly int win_s;
	private readonly int hop_s;
	private readonly Fft fft;
	private readonly float[] data;
	private readonly float[] dataold;
	private readonly float[] synth;
	private readonly float[] synthold;
	private readonly float[] w;
	private readonly int start;
	private readonly int end;
	private readonly float scale;
	public Pvoc(int win_s, int hop_s)
	{
		this.fft = new Fft(win_s);

		this.data = new float[win_s];
		this.synth = new float[win_s];

		this.dataold = new float[win_s];
		this.synthold = new float[win_s];
		this.w = Aubio.CreateWindow(WindowType.Hanning, win_s);

		this.hop_s = hop_s;
		this.win_s = win_s;

		if (win_s < 2 * hop_s) start = 0;
		else start = win_s - 2 * hop_s;

		if (win_s > hop_s) end = win_s - hop_s;
		else end = 0;

		scale = hop_s * 2f / win_s;
	}
	private void SwapBuffers(float[] vnew)
	{
		Array.Copy(dataold, data, end);
		Array.Copy(vnew, 0, data, end, hop_s);
		Array.Copy(data, hop_s, dataold, 0, end);
	}
	public void Do(float[] input, ComplexVector spectrum)
	{
		SwapBuffers(input);
		int length = int.Min(data.Length, w.Length);
		for (int j = 0; j < length; ++j)
			data[j] *= w[j];
		Aubio.Shift(data);
		fft.Do(data, spectrum);
	}
}
internal class AOnset
{
	private readonly Pvoc pv;
	private readonly Specdesc od;
	private readonly PeakPicker pp;
	private readonly ComplexVector fftgrain;
	private readonly float[] desc;
	private float silence;
	private int minioi;
	private int delay;
	private readonly int samplerate;
	private readonly int hop_size;
	private int total_frames;
	private int last_onset;
	public int Delay { get => delay; set => delay = value; }
	public float Threshold { get => pp.Threshold; set => pp.Threshold = value; }
	public TimeSpan Minioi { get => TimeSpan.FromSeconds(minioi / samplerate); set => minioi = (int)(value.TotalSeconds * samplerate); }
	public float Silence { get => silence; set => silence = value; }
	public AOnset(SpecdescMethod type, int buf_s, int hop_s, int samplerate)
	{
		this.samplerate = samplerate;
		this.hop_size = hop_s;

		pv = new(buf_s, hop_s);
		pp = new();
		od = new(type, buf_s);
		fftgrain = new ComplexVector(buf_s);
		desc = new float[1];

		Threshold = 0.3f;
		Delay = (int)(4.3f * hop_s);
		Minioi = TimeSpan.FromMilliseconds(20);
		Silence = -70f;

		last_onset = 0;
		total_frames = 0;
	}
	public void Do(float[] input, float[] onset)
	{
		float isonset;
		pv.Do(input, fftgrain);
		od.Do(fftgrain, desc);
		pp.Do(desc, onset);
		isonset = onset[0];
		if (isonset > 0f)
		{
			if (Aubio.SilenceDetection(input, silence))
				isonset = 0;
			else
			{
				int new_onset = total_frames + (int)float.Round(isonset * hop_size);
				if (last_onset + minioi < new_onset)
					last_onset = new_onset;
				else
					isonset = 0;
			}
		}
		else if (total_frames == 0 && !Aubio.SilenceDetection(input, silence))
		{
			isonset = delay / hop_size;
			last_onset = delay;
		}
		onset[0] = isonset;
		total_frames += hop_size;
	}
	public int GetLastOnsetSample() => last_onset - delay;
}
internal static class FindOnsets
{
	internal static SpecdescMethod method = SpecdescMethod.ComplexDomain;
	public static void Run(float[] samples, int samplerate, int numThreads, List<Onset> result)
	{
		const int windowlen = 256;
		const int bufsize = windowlen * 4;

		if (numThreads > 1)
		{
			int framesPerThread = samples.Length / numThreads;
			var concurrentOnsets = new ConcurrentBag<Onset>();

			Parallel.For(0, numThreads, thread =>
			{
				AOnset onset = new(method, bufsize, windowlen, samplerate);
				float[] samplevec = new float[windowlen];
				float[] beatvec = new float[2];

				int beginPos = framesPerThread * thread;
				int endPos = framesPerThread * (thread + 1);
				int paddedBegin = Math.Max(beginPos - bufsize, 0);
				int paddedEnd = Math.Min(endPos + bufsize, samples.Length - windowlen);

				for (int i = paddedBegin; i <= paddedEnd - windowlen; i += windowlen)
				{
					Array.Copy(samples, i, samplevec, 0, windowlen);
					onset.Do(samplevec, beatvec);
					if (beatvec[0] > 0f)
					{
						int pos = onset.GetLastOnsetSample() + paddedBegin;
						if (pos >= beginPos && pos < endPos)
							concurrentOnsets.Add(new(pos, 1.0));
					}
				}
			});
			// muitithread result is useless
			//result.AddRange(concurrentOnsets);
		}
		else
		{
			AOnset onset = new(method, bufsize, windowlen, samplerate);
			float[] samplevec = new float[windowlen], beatvec = new float[2];
			for (int i = 0; i <= samples.Length - windowlen; i += windowlen)
			{
				Array.Copy(samples, i, samplevec, 0, windowlen);
				onset.Do(samplevec, beatvec);
				if (beatvec[0] > 0f)
				{
					int pos = onset.GetLastOnsetSample();
					if (pos >= 0) result.Add(new(pos, 1.0));
				}
			}
		}
	}
}
