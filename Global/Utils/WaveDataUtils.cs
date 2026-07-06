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
		static readonly int fftLog2 = (int)Math.Log(fft, 2);
		const float fmin = 27.5f;    // 低频
		const float fmax = 800f;    // 高频
		static readonly float[] hannWindow = CreateHannWindow(fft);
		static readonly Dictionary<int, float[]> hannWindowCache = new Dictionary<int, float[]>();
		static readonly object hannWindowCacheLock = new object();
		static readonly Dictionary<string, MelBand[]> melFilterCache = new Dictionary<string, MelBand[]>();
		static readonly object melFilterCacheLock = new object();

		readonly struct MelBand
		{
			public readonly int Start;
			public readonly float[] Weights;

			public MelBand(int start, float[] weights)
			{
				Start = start;
				Weights = weights;
			}
		}
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
			const int framesPerRead = 4096;
			float[] floats = new float[framesPerRead * c];
			int read = 0;
			while ((read = provider.Read(floats, 0, floats.Length)) > 0)
			{
				for (int i = 0; i < read; i++)
					result[i % c].Add(floats[i]);
			}
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
			if (frames == 0)
				return mel;

			MelBand[] mfb = GetOrCreateMelFilterBank(sampleRate, fft, nMels, fmin, fmax);
			float[] frame = new float[fft];
			Complex[] fftBuffer = new Complex[fft];
			float[] magnitude = new float[fft / 2];

			for (int f = 0; f < frames; f++)
			{
				int start = f * hop;
				int copyLength = Math.Min(fft, signal.Length - start);
				Array.Copy(signal, start, frame, 0, copyLength);
				if (copyLength < fft)
					Array.Clear(frame, copyLength, fft - copyLength);

				MagnitudeSpectrum(frame, hannWindow, fftBuffer, magnitude);

				for (int m = 0; m < nMels; m++)
				{
					MelBand band = mfb[m];
					float sum = 0;
					for (int i = 0; i < band.Weights.Length; i++)
						sum += band.Weights[i] * magnitude[band.Start + i];
					mel[f, m] = sum;
				}
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
		/// Returns array of tuples: (bpm, peakValue, firstTickTimeSeconds)
		/// </summary>
		public static (double bpm, double score, int firstTickTimeFrame, double firstTickTimeSec)[] EstimateBPMWithOffset(float[] spectralFlux, int valueCount, double minbpm = 40, double maxbpm = 240)
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
			var results = new List<(double bpm, double score, int firstTickTimeFrame, double firstTickTimeSec)>();
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

				double firstTickTimeSec = bestFrame / (double)fps;
				results.Add((bpm, p.value * bestOffsetScore, bestFrame, firstTickTimeSec));
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
		public static float[] GetFrameFrequencyDomain(WaveFormat waveFormat, float[] samples, int maxFrequency = 2500, bool applyHannWindow = true)
		{
			if (samples.Length == 0)
				return Array.Empty<float>();

			GetPow2Params(samples.Length, out int newLen, out int log);
			Complex[] fftBuffer = new Complex[newLen];
			float[]? window = applyHannWindow ? GetOrCreateHannWindow(newLen) : null;
			return GetFrameFrequencyDomainInternal(waveFormat, samples, 0, samples.Length, maxFrequency, newLen, log, window, fftBuffer);
		}

		/// <summary>
		/// Get the frequency domain data of a range of sample.
		/// </summary>
		/// <param name="waveFormat">Wave format information.</param>
		/// <param name="timeDomainData">Audio sample.</param>
		/// <param name="windowWidth">The sample width.</param>
		/// <param name="maxFrequency">The maximum frequency to be retained.</param>
		/// <returns>An array with format [Channel][Frame][Frequency] data.</returns>
		public static float[][][] GetFrequencyDomain(WaveFormat waveFormat, float[][] timeDomainData, int windowWidth, int maxFrequency = 2500, bool applyHannWindow = true)
		{
			GetPow2Params(windowWidth, out int fftLen, out int fftLog);
			Complex[] fftBuffer = new Complex[fftLen];
			float[]? window = applyHannWindow ? GetOrCreateHannWindow(fftLen) : null;

			List<List<float[]>> result = new List<List<float[]>>();
			for (int i = 0; i < waveFormat.Channels; i++)
			{
				result.Add(new List<float[]>());
				float[] channelData = timeDomainData[i];
				int index = 0;
				while (index + windowWidth <= channelData.Length)
				{
					float[] outData = GetFrameFrequencyDomainInternal(waveFormat, channelData, index, windowWidth, maxFrequency, fftLen, fftLog, window, fftBuffer);
					result[i].Add(outData);
					index += windowWidth / 2;
				}
			}
			return result.Select(i => i.ToArray()).ToArray();
		}

		/// <summary>
		/// Convert a timestamp (seconds) to the nearest center-aligned frame index for outputs generated by GetFrequencyDomain.
		/// </summary>
		/// <param name="timeSeconds">Timestamp in seconds.</param>
		/// <param name="sampleRate">Audio sample rate.</param>
		/// <param name="windowWidth">Window width used by GetFrequencyDomain.</param>
		/// <returns>Clamped frame index.</returns>
		public static int TimeToFrameIndex(double timeSeconds, int sampleRate, int windowWidth)
		{
			if (sampleRate <= 0)
				throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
			if (windowWidth <= 0)
				throw new ArgumentOutOfRangeException(nameof(windowWidth), "Window width must be greater than zero.");

			double safeTime = Math.Max(0, timeSeconds);
			int hopLength = Math.Max(1, windowWidth / 2);
			int samplePosition = (int)Math.Round(safeTime * sampleRate);
			int frame = (int)Math.Round((samplePosition - windowWidth / 2.0) / hopLength);
			return frame;
		}

		/// <summary>
		/// Get the start/center/end timestamp (seconds) for a frame index generated by GetFrequencyDomain.
		/// </summary>
		/// <param name="frameIndex">Frame index.</param>
		/// <param name="sampleRate">Audio sample rate.</param>
		/// <param name="windowWidth">Window width used by GetFrequencyDomain.</param>
		/// <returns>(startSec, centerSec, endSec) for the specified frame.</returns>
		public static (double startSec, double centerSec, double endSec) FrameIndexToTimeRange(int frameIndex, int sampleRate, int windowWidth)
		{
			if (sampleRate <= 0)
				throw new ArgumentOutOfRangeException(nameof(sampleRate), "Sample rate must be greater than zero.");
			if (windowWidth <= 0)
				throw new ArgumentOutOfRangeException(nameof(windowWidth), "Window width must be greater than zero.");

			int safeFrame = Math.Max(0, frameIndex);
			int hopLength = Math.Max(1, windowWidth / 2);
			double startSec = safeFrame * hopLength / (double)sampleRate;
			double centerSec = (safeFrame * hopLength + windowWidth * 0.5) / sampleRate;
			double endSec = (safeFrame * hopLength + windowWidth) / (double)sampleRate;
			return (startSec, centerSec, endSec);
		}

		/// <summary>
		/// Reconstruct a mono time-domain signal from a magnitude spectrogram (frame x frequency bin).
		/// This is an approximate inversion that assumes zero phase.
		/// </summary>
		/// <param name="spectrogram">Magnitude spectrogram with shape [Frame][FrequencyBin].</param>
		/// <param name="hopLength">Hop length between adjacent frames.</param>
		/// <param name="applyHannWindow">Apply Hann window during overlap-add synthesis.</param>
		/// <param name="normalizePeak">Normalize peak to avoid clipping.</param>
		/// <returns>Reconstructed mono signal.</returns>
		public static float[] SpectrogramToAudio(float[][] spectrogram, int hopLength, bool applyHannWindow = true, bool normalizePeak = true)
		{
			if (spectrogram == null || spectrogram.Length == 0)
				return Array.Empty<float>();
			if (hopLength <= 0)
				throw new ArgumentOutOfRangeException(nameof(hopLength), "Hop length must be greater than zero.");

			int frames = spectrogram.Length;
			int bins = spectrogram[0]?.Length ?? 0;
			if (bins < 2)
				return Array.Empty<float>();

			int fftSize = (bins - 1) * 2;
			GetPow2Params(fftSize, out int checkedFftSize, out int fftLog);
			if (checkedFftSize != fftSize)
				throw new ArgumentException("Spectrogram bin count does not map to a power-of-two FFT size.", nameof(spectrogram));

			int outputLength = (frames - 1) * hopLength + fftSize;
			float[] output = new float[outputLength];
			float[] norm = new float[outputLength];
			float[]? window = applyHannWindow ? GetOrCreateHannWindow(fftSize) : null;
			Complex[] fftBuffer = new Complex[fftSize];

			for (int frameIndex = 0; frameIndex < frames; frameIndex++)
			{
				Array.Clear(fftBuffer, 0, fftBuffer.Length);
				float[] frame = spectrogram[frameIndex];
				if (frame == null)
					continue;

				int usefulBins = Math.Min(frame.Length, bins);
				for (int k = 0; k < usefulBins; k++)
				{
					float mag = Math.Max(0, frame[k]);
					fftBuffer[k].X = mag;
					fftBuffer[k].Y = 0;
					if (k > 0 && k < fftSize / 2)
					{
						fftBuffer[fftSize - k].X = mag;
						fftBuffer[fftSize - k].Y = 0;
					}
				}

				FastFourierTransform.FFT(false, fftLog, fftBuffer);

				int offset = frameIndex * hopLength;
				for (int n = 0; n < fftSize; n++)
				{
					float sample = fftBuffer[n].X / fftSize;
					float w = window != null ? window[n] : 1f;
					int idx = offset + n;
					output[idx] += sample * w;
					norm[idx] += w * w;
				}
			}

			for (int i = 0; i < output.Length; i++)
			{
				if (norm[i] > 1e-9f)
					output[i] /= norm[i];
			}

			if (normalizePeak)
			{
				float peak = 0f;
				for (int i = 0; i < output.Length; i++)
					peak = Math.Max(peak, Math.Abs(output[i]));
				if (peak > 1f)
				{
					float scale = 1f / peak;
					for (int i = 0; i < output.Length; i++)
						output[i] *= scale;
				}
			}

			return output;
		}

		/// <summary>
		/// Reconstruct a mono time-domain signal from one channel frequency-domain frames.
		/// This helper is intended for spectrograms generated by GetFrequencyDomain.
		/// </summary>
		/// <param name="frequencyDomainFrames">Frequency-domain frames with shape [Frame][FrequencyBin].</param>
		/// <param name="windowWidth">Window width used when generating frequency-domain frames.</param>
		/// <param name="applyHannWindow">Apply Hann window during overlap-add synthesis.</param>
		/// <param name="normalizePeak">Normalize peak to avoid clipping.</param>
		/// <returns>Reconstructed mono signal.</returns>
		public static float[] FrequencyDomainToAudio(float[][] frequencyDomainFrames, int windowWidth, bool applyHannWindow = true, bool normalizePeak = true)
		{
			if (windowWidth <= 0)
				throw new ArgumentOutOfRangeException(nameof(windowWidth), "Window width must be greater than zero.");

			int hopLength = Math.Max(1, windowWidth / 2);
			return SpectrogramToAudio(frequencyDomainFrames, hopLength, applyHannWindow, normalizePeak);
		}

		/// <summary>
		/// Reconstruct a mono time-domain signal from multi-channel frequency-domain frames.
		/// This helper is intended for outputs generated by GetFrequencyDomain.
		/// </summary>
		/// <param name="frequencyDomainData">Frequency-domain data with shape [Channel][Frame][FrequencyBin].</param>
		/// <param name="windowWidth">Window width used when generating frequency-domain frames.</param>
		/// <param name="mixDownChannels">Mix all channels down to mono.</param>
		/// <param name="applyHannWindow">Apply Hann window during overlap-add synthesis.</param>
		/// <param name="normalizePeak">Normalize peak to avoid clipping.</param>
		/// <returns>Reconstructed mono signal.</returns>
		public static float[] FrequencyDomainToAudio(float[][][] frequencyDomainData, int windowWidth, bool mixDownChannels = true, bool applyHannWindow = true, bool normalizePeak = true)
		{
			if (frequencyDomainData == null || frequencyDomainData.Length == 0)
				return Array.Empty<float>();
			if (windowWidth <= 0)
				throw new ArgumentOutOfRangeException(nameof(windowWidth), "Window width must be greater than zero.");

			if (!mixDownChannels || frequencyDomainData.Length == 1)
				return FrequencyDomainToAudio(frequencyDomainData[0], windowWidth, applyHannWindow, normalizePeak);

			float[] first = FrequencyDomainToAudio(frequencyDomainData[0], windowWidth, applyHannWindow, false);
			if (first.Length == 0)
				return first;

			float[] mixed = new float[first.Length];
			Array.Copy(first, mixed, first.Length);

			for (int ch = 1; ch < frequencyDomainData.Length; ch++)
			{
				float[] channelAudio = FrequencyDomainToAudio(frequencyDomainData[ch], windowWidth, applyHannWindow, false);
				int len = Math.Min(mixed.Length, channelAudio.Length);
				for (int i = 0; i < len; i++)
					mixed[i] += channelAudio[i];
			}

			float inv = 1f / frequencyDomainData.Length;
			for (int i = 0; i < mixed.Length; i++)
				mixed[i] *= inv;

			if (normalizePeak)
			{
				float peak = 0f;
				for (int i = 0; i < mixed.Length; i++)
					peak = Math.Max(peak, Math.Abs(mixed[i]));
				if (peak > 1f)
				{
					float scale = 1f / peak;
					for (int i = 0; i < mixed.Length; i++)
						mixed[i] *= scale;
				}
			}

			return mixed;
		}

		private static float[] GetFrameFrequencyDomainInternal(
			WaveFormat waveFormat,
			float[] source,
			int sourceOffset,
			int sampleLength,
			int maxFrequency,
			int fftLen,
			int fftLog,
			float[]? window,
			Complex[] fftBuffer)
		{
			for (int i = 0; i < fftLen; i++)
			{
				float value = i < sampleLength ? source[sourceOffset + i] : 0;
				if (window != null)
					value *= window[i];
				fftBuffer[i].X = value;
				fftBuffer[i].Y = 0;
			}

			// Keep FFT direction consistent with other spectrum analysis paths.
			FastFourierTransform.FFT(true, fftLog, fftBuffer);

			int halfLength = fftLen / 2;
			int count = Math.Max(0, (int)(maxFrequency / (waveFormat.SampleRate / (double)fftLen)));
			count = Math.Min(count, halfLength);
			float[] finalData = new float[count];
			for (int i = 0; i < count; i++)
			{
				float x = fftBuffer[i].X;
				float y = fftBuffer[i].Y;
				finalData[i] = (float)Math.Sqrt(x * x + y * y);
			}

			return finalData;
		}

		private static void GetPow2Params(int inputLength, out int len, out int log)
		{
			len = 1;
			log = 0;
			while (len < inputLength)
			{
				len <<= 1;
				log++;
			}
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
		public static (double bpm, double firstTickTimeSec) Process(string file)
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
			Console.WriteLine(string.Join("\n", candidates.Select(c => $"BPM={c.bpm:F2}, score={c.score:F4}, firstTickTimeSec={c.firstTickTimeSec:F3}")));
			return (best.bpm, best.firstTickTimeSec);
		}
		private static void MagnitudeSpectrum(float[] frame, float[] window, Complex[] fftBuffer, float[] output)
		{
			for (int i = 0; i < fft; i++)
			{
				fftBuffer[i].X = frame[i] * window[i];
				fftBuffer[i].Y = 0;
			}

			FastFourierTransform.FFT(true, fftLog2, fftBuffer);
			for (int i = 0; i < output.Length; i++)
			{
				float x = fftBuffer[i].X;
				float y = fftBuffer[i].Y;
				output[i] = (float)Math.Sqrt(x * x + y * y);
			}
		}

		private static MelBand[] GetOrCreateMelFilterBank(int sampleRate, int fftBins, int melBins, float lowFreq, float highFreq)
		{
			string key = $"{sampleRate}_{fftBins}_{melBins}_{lowFreq:F3}_{highFreq:F3}";
			lock (melFilterCacheLock)
			{
				if (!melFilterCache.TryGetValue(key, out MelBand[]? bank))
				{
					bank = MelFilterBankSparse(sampleRate, fftBins, melBins, lowFreq, highFreq);
					melFilterCache[key] = bank;
				}
				return bank!;
			}
		}

		private static MelBand[] MelFilterBankSparse(int sampleRate, int fftBins, int melBins, float lowFreq, float highFreq)
		{
			int spectrumSize = fftBins / 2;
			MelBand[] bank = new MelBand[melBins];

			float melMin = 2595 * MathF.Log10(1 + lowFreq / 700);
			float melMax = 2595 * MathF.Log10(1 + highFreq / 700);
			float[] melPoints = new float[melBins + 2];
			float[] bins = new float[melBins + 2];

			for (int i = 0; i < melPoints.Length; i++)
			{
				melPoints[i] = melMin + i * (melMax - melMin) / (melBins + 1);
				bins[i] = (fftBins + 1) * (700 * (MathF.Pow(10, melPoints[i] / 2595) - 1)) / sampleRate;
			}

			for (int i = 0; i < melBins; i++)
			{
				float left = bins[i];
				float center = bins[i + 1];
				float right = bins[i + 2];

				int start = Math.Max(0, (int)Math.Ceiling(left));
				int centerBin = (int)Math.Floor(center);
				int end = Math.Min(spectrumSize - 1, (int)Math.Floor(right));

				if (end < start)
				{
					bank[i] = new MelBand(0, Array.Empty<float>());
					continue;
				}

				float[] weights = new float[end - start + 1];
				for (int k = start; k <= end; k++)
				{
					float weight;
					if (k <= centerBin)
					{
						weight = center > left ? (k - left) / (center - left) : 0;
					}
					else
					{
						weight = right > center ? (right - k) / (right - center) : 0;
					}

					weights[k - start] = Math.Max(0, weight);
				}

				bank[i] = new MelBand(start, weights);
			}

			return bank;
		}

		private static float[] CreateHannWindow(int size)
		{
			float[] window = new float[size];
			if (size <= 1)
				return window;

			for (int i = 0; i < size; i++)
				window[i] = 0.5f - 0.5f * MathF.Cos(2 * MathF.PI * i / (size - 1));

			return window;
		}

		private static float[] GetOrCreateHannWindow(int size)
		{
			lock (hannWindowCacheLock)
			{
				if (!hannWindowCache.TryGetValue(size, out float[]? window))
				{
					window = CreateHannWindow(size);
					hannWindowCache[size] = window;
				}

				return window!;
			}
		}
	}
}