using NAudio.Dsp;
using NAudio.Vorbis;
using NAudio.Wave;
using RhythmBase.Global.Extensions;
using SkiaSharp;
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace RhythmBase.Global.Utils
{
	/// <summary>
	/// Wave data utils.
	/// </summary>
	public static class WaveDataUtils
	{
		const int sr = 44100;   // 采样率
		const int fps = 200;     // 每秒 100 帧
		const int hop = sr / fps; // 441
		const int nMels = 120;      // 低频带通
		const int fft = 4096;
		const float fmin = 27.5f;    // 低频
		const float fmax = 800f;    // 高频
		/// <summary>
		/// Get wave stream of the audio file.
		/// </summary>
		/// <param name="filepath">Audio file path.</param>
		/// <returns>The wave stream of the audio file.</returns>
		public static WaveStream GetWaveStream(string filepath)
		{
			if (File.Exists(filepath))
			{
				string extension = Path.GetExtension(filepath).ToLowerInvariant();
				return extension switch
				{
					".ogg" => new VorbisWaveReader(filepath),
					".mp3" => new Mp3FileReader(filepath),
					".wav" or ".wave" => new WaveFileReader(filepath),
					".aiff" or ".aif" => new AiffFileReader(filepath),
					_ => throw new NotSupportedException(extension),
				};
			}
			return (WaveStream)Stream.Null;
		}

		/// <summary>
		/// Get the time domain data of the stream.
		/// </summary>
		/// <param name="stream">Wave stream.</param>
		/// <returns>A 2D array with format [Channel][Frame] data.</returns>
		/// <exception cref="NotSupportedException">The encoding format was not supported yet.</exception>
		public static float[][] GetTimeDomain(this WaveStream stream)
		{
			int c = stream.WaveFormat.Channels;
			List<List<float>> result = new List<List<float>>();
			for (int i = 0; i < c; i++)
				result.Add(new List<float>());

			ISampleProvider provider = stream.ToSampleProvider();
			float[] floats = new float[c];
			int read = 0;
			while ((read = provider.Read(floats, 0, c)) > 0)
				for (int i = 0; i < c; i++)
					result[i].Add(floats[i]);
			return result.Select(i => i.ToArray()).ToArray();
		}
		public static float[] GetMonoTimeDomain(this WaveStream stream)
		{
			float[][] timeDomain = GetTimeDomain(stream);
			return Average(timeDomain);
		}
		private static float[,] MelSpectrogram(float[] signal, int sampleRate)
		{
			int frames = Math.Max(0, (signal.Length - fft) / hop + 1);
			float[,] mel = new float[frames, nMels];

			var mfb = MelFilterBank(sampleRate, fft, nMels, fmin, fmax);

			for (int f = 0; f < frames; f++)
			{
				float[] frame = new float[fft];
				Array.Copy(signal, f * hop, frame, 0, Math.Min(fft, signal.Length - f * hop));

				float[] mag = MagnitudeSpectrum(frame);
				for (int m = 0; m < nMels; m++)
					mel[f, m] = mfb[m].Dot(mag);
			}
			return mel;
		}
		private static float[] SpectralFlux(float[,] mel)
		{
			int frames = mel.GetLength(0);
			float[] flux = new float[frames];

			for (int t = 1; t < frames; t++)
			{
				float sum = 0;
				for (int m = 0; m < nMels; m++)
					sum += Math.Max(0, mel[t, m] - mel[t - 1, m]);
				flux[t] = sum;
			}
			return flux;
		}
		public static double[] EstimateBPM(float[] spectralFlux, int valueCount, double minbpm = 50, double maxbpm = 240)
		{
			if (spectralFlux == null || spectralFlux.Length < 3)
				return Array.Empty<double>();

			int N = spectralFlux.Length;
			double[] x = new double[N];
			for (int i = 0; i < N; i++) x[i] = spectralFlux[i];
			double mean = x.Average();
			for (int i = 0; i < N; i++) x[i] -= mean;
			for (int i = 0; i < N; i++) x[i] = Math.Max(0.0, x[i]);
			double maxv = x.Max();
			if (maxv > 0)
				for (int i = 0; i < N; i++) x[i] /= maxv;

			int minLag = (int)Math.Floor(60.0 * fps / maxbpm);
			int maxLag = (int)Math.Ceiling(60.0 * fps / minbpm);
			minLag = Math.Max(1, minLag);
			maxLag = Math.Min(maxLag, N - 1);
			if (maxLag <= minLag)
				return Array.Empty<double>();

			double[] autocorr = new double[maxLag + 1]; // index by lag
			for (int lag = 1; lag <= maxLag; lag++)
			{
				double sum = 0;
				int limit = N - lag;
				for (int t = 0; t < limit; t++)
					sum += x[t] * x[t + lag];
				autocorr[lag] = (limit > 0) ? sum / limit : 0.0;
			}

			var peaks = new List<(int lag, double value)>();
			for (int lag = minLag; lag <= maxLag; lag++)
			{
				double val = autocorr[lag];
				double left = (lag - 1 >= 1) ? autocorr[lag - 1] : double.MinValue;
				double right = (lag + 1 <= maxLag) ? autocorr[lag + 1] : double.MinValue;
				if (val > left && val > right)
					peaks.Add((lag, val));
			}

			if (peaks.Count == 0)
			{
				for (int lag = minLag; lag <= maxLag; lag++)
					peaks.Add((lag, autocorr[lag]));
			}

			var top = peaks.OrderByDescending(p => p.value).Take(valueCount).ToArray();

			var bpms = new List<double>();
			foreach (var p in top)
			{
				int lag = p.lag;
				double y0 = autocorr[lag];
				double ym = (lag - 1 >= 1) ? autocorr[lag - 1] : 0.0;
				double yp = (lag + 1 <= maxLag) ? autocorr[lag + 1] : 0.0;
				double denom = (ym - 2 * y0 + yp);
				double delta = 0.0;
				if (Math.Abs(denom) > 1e-9)
					delta = 0.5 * (ym - yp) / denom;
				double refinedLag = lag + delta;
				if (refinedLag <= 0) refinedLag = lag;
				double bpm = 60.0 * fps / refinedLag;
				bpms.Add(bpm);
			}

			return bpms.ToArray();
		}
		/// <summary>
		/// Estimate BPM candidates and find best phase offset (first beat) for each candidate.
		/// Returns array of tuples: (bpm, peakValue, firstBeatSeconds)
		/// </summary>
		public static (double bpm, double score, int firstBeatFrame, double firstBeatSec)[] EstimateBPMWithOffset(float[] spectralFlux, int valueCount, double minbpm = 40, double maxbpm = 240)
		{
			if (spectralFlux == null || spectralFlux.Length < 3)
				return Array.Empty<(double, double, int, double)>();

			int N = spectralFlux.Length;
			double[] x = new double[N];
			for (int i = 0; i < N; i++) x[i] = spectralFlux[i];
			double mean = x.Average();
			for (int i = 0; i < N; i++) x[i] -= mean;
			for (int i = 0; i < N; i++) x[i] = Math.Max(0.0, x[i]);
			double maxv = x.Max(); if (maxv > 0) for (int i = 0; i < N; i++) x[i] /= maxv;

			double[] xsm = new double[N];
			for (int i = 0; i < N; i++)
			{
				double a = x[i];
				double b = (i - 1 >= 0) ? x[i - 1] : 0.0;
				double c = (i + 1 < N) ? x[i + 1] : 0.0;
				xsm[i] = (a + b + c) / 3.0;
			}
			double xsmStd = Math.Sqrt(Math.Max(1e-12, xsm.Select(v => v * v).Average()));

			int minLag = (int)Math.Floor(60.0 * fps / maxbpm);
			int maxLag = (int)Math.Ceiling(60.0 * fps / minbpm);
			minLag = Math.Max(1, minLag);
			maxLag = Math.Min(maxLag, N - 1);
			if (maxLag <= minLag)
				return Array.Empty<(double, double, int, double)>();

			double[] autocorr = new double[maxLag + 1];
			for (int lag = 1; lag <= maxLag; lag++)
			{
				double sum = 0; int limit = N - lag;
				for (int t = 0; t < limit; t++) sum += x[t] * x[t + lag];
				autocorr[lag] = (limit > 0) ? sum / limit : 0.0;
			}

			var peaks = new List<(int lag, double value)>();
			for (int lag = minLag; lag <= maxLag; lag++)
			{
				double val = autocorr[lag];
				double left = (lag - 1 >= 1) ? autocorr[lag - 1] : double.MinValue;
				double right = (lag + 1 <= maxLag) ? autocorr[lag + 1] : double.MinValue;
				if (val > left && val > right) peaks.Add((lag, val));
			}
			if (peaks.Count == 0) for (int lag = minLag; lag <= maxLag; lag++) peaks.Add((lag, autocorr[lag]));

			var top = peaks.OrderByDescending(p => p.value).Take(valueCount).ToArray();
			var results = new List<(double bpm, double score, int firstBeatFrame, double firstBeatSec)>();
			foreach (var p in top)
			{
				int lag = p.lag;
				double y0 = autocorr[lag];
				double ym = (lag - 1 >= 1) ? autocorr[lag - 1] : 0.0;
				double yp = (lag + 1 <= maxLag) ? autocorr[lag + 1] : 0.0;
				double denom = (ym - 2 * y0 + yp);
				double delta = 0.0;
				if (Math.Abs(denom) > 1e-9) delta = 0.5 * (ym - yp) / denom;
				double refinedLag = Math.Max(1.0, lag + delta);
				double bpm = 60.0 * fps / refinedLag;

				int intLag = (int)Math.Round(refinedLag);
				if (intLag < 1) intLag = 1;

				double bestOffsetScore = double.MinValue;
				int bestOffset = 0;
				for (int offset = 0; offset < intLag; offset++)
				{
					double scoreSum = 0.0; int count = 0;
					for (int pos = offset; pos < N; pos += intLag)
					{
						double local = 0.0;
						if (pos - 1 >= 0) local += 0.25 * xsm[pos - 1];
						local += 0.5 * xsm[pos];
						if (pos + 1 < N) local += 0.25 * xsm[pos + 1];
						scoreSum += local;
						count++;
					}
					if (count == 0) continue;
					double avg = scoreSum / count;
					double normScore = avg / (xsmStd + 1e-12);
					if (normScore > bestOffsetScore) { bestOffsetScore = normScore; bestOffset = offset; }
				}

				int bestFrame = bestOffset;
				double bestFrameVal = xsm[bestFrame];
				for (int pos = bestOffset; pos < N; pos += intLag)
				{
					if (xsm[pos] > bestFrameVal)
					{
						bestFrameVal = xsm[pos];
						bestFrame = pos;
					}
				}

				double firstBeatSec = bestFrame / (double)fps;
				results.Add((bpm, p.value * bestOffsetScore, bestFrame, firstBeatSec));
			}

			return results.ToArray();
		}

		/// <summary>
		/// Calculate the average of the time domain data.
		/// </summary>
		/// <param name="timeDomain">Time domain data.</param>
		/// <returns>An array with the average values of each frame.</returns>
		public static float[] Average(this float[][] timeDomain) => Enumerable.Range(0, timeDomain[0].Length)
				.Select(i => Enumerable.Range(0, timeDomain.Length)
					.Select(c => timeDomain[c][i])
					.Average())
				.ToArray();

		/// <summary>
		/// Get the time domain data of the stream which is compressed in average.
		/// </summary>
		/// <param name="stream">Wave stream.</param>
		/// <returns>An array with format [Frame] data.</returns>
		/// <exception cref="NotSupportedException">The encoding format was not supported yet.</exception>
		public static float[] GetAverageTimeDomain(this WaveStream stream)
		{
			List<float> result = new List<float>();
			int BytesPerSample = stream.WaveFormat.BitsPerSample / 8;
			long byteLen = stream.Length;
			byte[] sample = new byte[byteLen];
			stream.Read(sample, 0, sample.Length);
			for (int i = 0; i + BytesPerSample * stream.WaveFormat.Channels <= sample.Length; i += BytesPerSample * stream.WaveFormat.Channels)
			{
				List<float> channelsData = new List<float>();
				int offset = i;
				for (int c = 0; c < stream.WaveFormat.Channels; c++)
				{
					float value = stream.WaveFormat.Encoding switch
					{
						WaveFormatEncoding.IeeeFloat => BitConverter.ToSingle(sample, offset),
						WaveFormatEncoding.Pcm => BitConverter.ToInt16(sample, offset) / (float)short.MaxValue,
						_ => throw new NotSupportedException(stream.WaveFormat.Encoding.ToString()),
					};
					channelsData.Add(value);
					offset += BytesPerSample;
				}
				result.Add(channelsData.Average());
			}
			stream.Position = 0;
			return result.ToArray();
		}

		/// <summary>
		/// Get a frame of the frequency domain data of a range of sample.
		/// </summary>
		/// <param name="waveFormat">Wave format information.</param>
		/// <param name="samples">Audio sample.</param>
		/// <param name="maxFrequency">The maximum frequency to be retained.</param>
		/// <returns>An array with format [Frequency] data.</returns>
		public static float[] GetFrameFrequencyDomain(WaveFormat waveFormat, float[] samples, int maxFrequency = 2500)
		{
			List<float[]> finalDatas = new List<float[]>();
			int log = (int)Math.Ceiling(Math.Log(samples.Length, 2));
			int newLen = (int)Math.Pow(2, log);
			float[] filledSamples = new float[newLen];
			Array.Copy(samples, filledSamples, samples.Length);
			Complex[] complexSrc = filledSamples
				.Select(v => new Complex() { X = v })
				.ToArray();
			FastFourierTransform.FFT(false, log, complexSrc);

			Complex[] halfData = complexSrc
				.Take(complexSrc.Length / 2)
				.ToArray();
			float[] dftData = halfData
				.Select(v => (float)Math.Sqrt(v.X * v.X + v.Y * v.Y))
				.ToArray();

			int count = Math.Max(0, (int)(maxFrequency / (waveFormat.SampleRate / (double)filledSamples.Length)));
			float[] finalData = dftData.Take(count).ToArray();
			finalDatas.Add(dftData);

			return finalData;
		}

		/// <summary>
		/// Get the frequency domain data of a range of sample.
		/// </summary>
		/// <param name="waveFormat">Wave format information.</param>
		/// <param name="timeDomainData">Audio sample.</param>
		/// <param name="windowWidth">The sample width.</param>
		/// <param name="maxFrequency">The maximum frequency to be retained.</param>
		/// <returns>An array with format [Channel][Frame][Frequency] data.</returns>
		public static float[][][] GetFrequencyDomain(WaveFormat waveFormat, float[][] timeDomainData, int windowWidth, int maxFrequency = 2500)
		{
			List<List<float[]>> result = new List<List<float[]>>();
			for (int i = 0; i < waveFormat.Channels; i++)
			{
				result.Add(new List<float[]>());
				int index = 0;
				while (index + windowWidth <= timeDomainData[i].Length)
				{
					float[] buffer = timeDomainData[i].Skip(index).Take(windowWidth).ToArray();
					index += windowWidth / 2;
					float[] outData = GetFrameFrequencyDomain(waveFormat, buffer, maxFrequency);
					result[i].Add(outData);
				}
			}
			return result.Select(i => i.ToArray()).ToArray();
		}

		/// <summary>
		/// Converts time domain data to energy.
		/// </summary>
		/// <param name="waveFormat">Wave format information.</param>
		/// <param name="timeDomainData">Time domain data.</param>
		/// <param name="windowWidth">The width of the window for energy calculation.</param>
		/// <returns>A 2D array with format [Channel][Energy] data.</returns>
		public static float[][] ToEnergy(WaveFormat waveFormat, float[][] timeDomainData, int windowWidth)
		{
			List<float[]> result = new List<float[]>();
			for (int channel = 0; channel < waveFormat.Channels; channel++)
			{
				List<float> volume = new List<float>();
				for (int j = 0; j + windowWidth <= timeDomainData[channel].Length; j += windowWidth / 2)
				{
					volume.Add((float)Math.Sqrt(timeDomainData[channel].Skip(j).Take(windowWidth).Select(i => i * i).Average()));
				}
				result.Add(volume.ToArray());
			}
			return result.ToArray();
		}
		public static (double bpm, double firstBeatSec) Process(string file)
		{
			using var ws = GetWaveStream(file);
			float[] mono = ws.GetMonoTimeDomain();

			float[,] mel = MelSpectrogram(mono, ws.WaveFormat.SampleRate);

			float[] flux = SpectralFlux(mel);

			var candidates = EstimateBPMWithOffset(flux, 10);
			if (candidates.Length == 0)
			{
				return (0, 0);
			}

			var best = candidates.OrderByDescending(c => c.score).First();
			Console.WriteLine(string.Join("\n", candidates.Select(c => $"BPM={c.bpm:F2}, score={c.score:F4}, firstBeatSec={c.firstBeatSec:F3}")));
			return (best.bpm, best.firstBeatSec);
		}
		private static float[] MagnitudeSpectrum(float[] frame)
		{
			int n = fft;
			Complex[] c = frame.Select(v => new Complex { X = v }).ToArray();
			FastFourierTransform.FFT(true, (int)Math.Log(n, 2), c);
			return c.Take(n / 2).Select(x => (float)Math.Sqrt(x.X * x.X + x.Y * x.Y)).ToArray();
		}
		private static float[][] MelFilterBank(int sr, int fftBins, int nMels, float fMin, float fMax)
		{
			int nyq = sr / 2;
			int m = fftBins / 2;                 // 频率点数
			float[][] bank = new float[nMels][];

			float melMin = 2595 * MathF.Log10(1 + fMin / 700);
			float melMax = 2595 * MathF.Log10(1 + fMax / 700);

			float[] melPoints = Enumerable.Range(0, nMels + 2)
								.Select(i => melMin + i * (melMax - melMin) / (nMels + 1))
								.ToArray();
			float[] bin = melPoints.Select(mp => ((fftBins + 1) * (700 * (MathF.Pow(10, mp / 2595) - 1)) / sr))
						.ToArray();

			for (int i = 0; i < nMels; i++)
			{
				float[] filt = new float[m];
				float left = bin[i], center = bin[i + 1], right = bin[i + 2];

				int start = Math.Max(0, (int)Math.Ceiling(left));
				int cen = (int)Math.Floor(center);
				int end = Math.Min(m - 1, (int)Math.Floor(right));

				for (int k = start; k <= cen && k < m; k++)
					filt[k] = (k - left) / (float)(center - left);
				for (int k = Math.Max(cen + 1, 0); k <= end; k++)
					filt[k] = (right - k) / (float)(right - center);

				bank[i] = filt;
			}
			return bank;
		}
		private static float Dot(this float[] a, float[] b)
		{
			if (a.Length != b.Length)
				throw new ArgumentException("Vectors must have the same length.");
			float result = 0;
			for (int i = 0; i < a.Length; i++)
			{
				result += a[i] * b[i];
			}
			return result;
		}
	}
}