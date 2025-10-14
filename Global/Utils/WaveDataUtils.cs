using NAudio.Dsp;
using NAudio.Vorbis;
using NAudio.Wave;
using RhythmBase.Global.Extensions;
using SkiaSharp;
using System.Data;

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
			if (Path.Exists(filepath))
			{
				string extension = Path.GetExtension(filepath);
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
			List<List<float>> result = [];
			for (int i = 0; i < c; i++)
				result.Add([]);

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
			int frames = (signal.Length - fft) / hop + 1;
			float[,] mel = new float[frames, nMels];

			// 预计算梅尔滤波器组
			var mfb = MelFilterBank(sampleRate, fft, nMels, fmin, fmax);
			//mfb.OutputData2DBitmap("mel.png", SKColors.Green, SKColors.Black);

			for (int f = 0; f < frames; f++)
			{
				float[] frame = new float[fft];
				Array.Copy(signal, f * hop, frame, 0, fft);

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
		public static double[] EstimateBPM(float[] spectralFlux, int valueCount, double minbpm = 40, double maxbpm = 240)
		{
			double maxLag = fps * 10; // 最大周期 10 秒
			double minLag = 60.0 * fps / maxbpm;
			double[] autocorrelation = new double[((int)maxLag-1)];

			for (int lag = 1; lag < maxLag; lag++)
			{
				double sum = 0;
				for (int t = 0; t < spectralFlux.Length - lag; t++)
				{
					sum += spectralFlux[t] * spectralFlux[t + lag];
				}
				autocorrelation[lag-1] = sum / (spectralFlux.Length - lag);

			}

			double[] copy = new double[autocorrelation.Length];
			//autocorrelation.CopyTo(copy, 0);
			//for(int i=0;i<copy.Length;i++)
			//{
			//	if ((i == 0 || (i > 0 && autocorrelation[i - 1] < autocorrelation[i])) &&
			//		(i == copy.Length - 1 || (i < copy.Length - 1 && autocorrelation[i + 1] < autocorrelation[i])))
			//	{
			//		copy[i] = autocorrelation[i];
			//	}
			//}
			autocorrelation.Output1DDataBitmap("out3.png", 500, SKColors.Green, SKColors.Black);

			maxLag = 60.0 * fps / minbpm;

			double[] lags = autocorrelation[(int)double.Floor(minLag)..(int)double.Ceiling(maxLag)];
			double[] bestMatches = [.. lags.Order().Take(valueCount)];
			double[] bpms = [.. bestMatches.Select(i=> 60.0 * fps / Array.IndexOf(autocorrelation, i))];

			//int bestLag = Array.IndexOf(autocorrelation, autocorrelation.su.Max());
			//double bpm = 60.0 * fps / bestLag;

			return bpms;
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
			List<float> result = [];
			int BytesPerSample = stream.WaveFormat.BitsPerSample / 8;
			byte[] sample = new byte[stream.Length];
			stream.Read(sample, 0, sample.Length);
			for (int i = 0; i < sample.Length; i += BytesPerSample)
			{
				List<float> channelsData = [];
				for (int c = 0; c < stream.WaveFormat.Channels; c++)
				{
					float value = stream.WaveFormat.Encoding switch
					{
						WaveFormatEncoding.IeeeFloat => BitConverter.ToSingle(sample, i),
						WaveFormatEncoding.Pcm => BitConverter.ToInt16(sample, i) / (float)short.MaxValue,
						_ => throw new NotSupportedException(stream.WaveFormat.Encoding.ToString()),
					};
					channelsData.Add(value);
				}
				result.Add(channelsData.Average());
			}
			stream.Position = 0;
			return [.. result];
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
			List<float[]> finalDatas = [];
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

			int count = maxFrequency / (waveFormat.SampleRate / filledSamples.Length);
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
			List<List<float[]>> result = [];
			int index = 0;
			float[] buffer = new float[windowWidth];
			for (int i = 0; i < waveFormat.Channels; i++)
			{
				result.Add([]);
				while (index + windowWidth < timeDomainData[i].Length)
				{
					buffer = timeDomainData[i][index..(index + windowWidth - 1)];
					index += windowWidth / 2;
					float[] outData = GetFrameFrequencyDomain(waveFormat, buffer, maxFrequency);
					result[i].Add(outData);
				}
				index = 0;
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
			List<float[]> result = [];
			for (int channel = 0; channel < waveFormat.Channels; channel++)
			{
				List<float> volume = [];
				for (int j = 0; j < timeDomainData.Length - windowWidth; j += windowWidth / 2)
				{
					volume.Add((float)Math.Sqrt(timeDomainData[channel][j..(j + windowWidth)].Select(i => i * i).Average()));
				}
				result.Add([.. volume]);
			}
			return [.. result];
		}
		public static (double bpm, double firstBeatSec) Process(string file)
		{
			using var ws = GetWaveStream(file);
			float[] mono = ws.GetMonoTimeDomain();

			// 1) 低频梅尔谱
			float[,] mel = MelSpectrogram(mono, ws.WaveFormat.SampleRate);
			//mel.OutputData2DBitmap("out1.png", SKColors.Green, SKColors.Black);

			// 2) Spectral Flux
			float[] flux = SpectralFlux(mel);
			//flux.Output1DDataBitmap("out2.png", 2000, SKColors.Green, SKColors.Black);

			// 3) 全局 BPM（自相关峰值）
			double[] bpm = EstimateBPM(flux, 5);
			Console.WriteLine(string.Join(",", bpm.Select(i=>$"{i:F2}")));


			//// 4) 动态规划找首拍
			//double[] beats = DPBeats(flux, bpm);
			//double firstBeat = beats.Length > 0 ? beats[0] : 0;

			//return (bpm, firstBeat);

			return (0, 0);
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

			// mel 刻度端点
			float melMin = 2595 * MathF.Log10(1 + fMin / 700);
			float melMax = 2595 * MathF.Log10(1 + fMax / 700);

			// 线性插值到 nMels+2 个点
			float[] melPoints = Enumerable.Range(0, nMels + 2)
										  .Select(i => melMin + i * (melMax - melMin) / (nMels + 1))
										  .ToArray();
			float[] bin = melPoints.Select(m => ((fftBins + 1) * (700 * (MathF.Pow(10, m / 2595) - 1)) / sr))
								 .ToArray();

			for (int i = 0; i < nMels; i++)
			{
				float[] filt = new float[m];
				float left = bin[i], center = bin[i + 1], right = bin[i + 2];

				for (int k = (int)float.Ceiling(left); k <= float.Floor(center); k++)
					filt[k] = (k - left) / (float)(center - left);
				for (int k = (int)float.Ceiling(center); k <= float.Floor(right); k++)
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
		private static void Output1DDataBitmap(this float[] data, string fileName, int size, SKColor foreground, SKColor background) => Output1DDataBitmap(data.Select(i => (double)i).ToArray(), fileName, size, foreground, background);
		private static void Output1DDataBitmap(this double[] data, string fileName, int size, SKColor foreground, SKColor background)
		{
			SKBitmap bitmap = new(data.Length, size);
			SKCanvas canvas = new(bitmap);
			SKPaint paint = new() { Color = foreground };
			canvas.Clear(background);
			double min = data.Min();
			double max = data.Max();
			for (int i = 0; i < data.Length; i++)
			{
				canvas.DrawLine(new(i, size), new(i, size - (int)(size * (data[i] - min) / (max - min))), paint);
			}
			bitmap.Save(fileName);
		}
		private static void OutputData2DBitmap(this float[,] data, string filename, SKColor foreground, SKColor background)
		{
			int width = data.GetLength(0);
			int height = data.GetLength(1);
			SKBitmap bitmap = new(width, height);
			SKCanvas canvas = new(bitmap);
			canvas.Clear(background);
			float min = data.Cast<float>().Min();
			float max = data.Cast<float>().Max();
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					double value = data[x, y];
					canvas.DrawPoint(new(x, y), background.Mix(foreground, (float)((value - min) / (max - min))));
				}
			}
			bitmap.Save(filename);
		}
		private static void OutputData2DBitmap(this double[,] data, string filename, SKColor foreground, SKColor background)
		{
			int width = data.GetLength(0);
			int height = data.GetLength(1);
			SKBitmap bitmap = new(width, height);
			SKCanvas canvas = new(bitmap);
			canvas.Clear(background);
			double min = data.Cast<double>().Min();
			double max = data.Cast<double>().Max();
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					double value = data[x, y];
					if (value > 0)
						canvas.DrawPoint(new(x, y), foreground.WithAlpha((byte)((value - min) / (max - min))));
				}
			}
			bitmap.Save(filename);
		}
		private static void OutputData2DBitmap(this float[][] data, string filename, SKColor foreground, SKColor background) => data.Select(i => i.Select(j => (double)j).ToArray()).ToArray().OutputData2DBitmap(filename, foreground, background);
		private static void OutputData2DBitmap(this double[][] data, string filename, SKColor foreground, SKColor background)
		{
			int width = data.Length;
			int height = data.Max(i => i.Length);
			SKBitmap bitmap = new(width, height);
			SKCanvas canvas = new(bitmap);
			SKPaint paint = new() { Color = foreground };
			canvas.Clear(background);
			double min = data.Min(i => i.Min());
			double max = data.Max(i => i.Max());
			for (int x = 0; x < width; x++)
			{
				for (int y = 0; y < height; y++)
				{
					double value = data[x][y];
					canvas.DrawPoint(new(x, y), background.Mix(foreground, (float)((value - min) / (max - min))));
				}
			}
			bitmap.Save(filename);
		}
		private static SKColor Mix(this SKColor e, SKColor f, float rate)
		{
			float r = e.Red * (1 - rate) + f.Red * rate;
			float g = e.Green * (1 - rate) + f.Green * rate;
			float b = e.Blue * (1 - rate) + f.Blue * rate;
			float a = e.Alpha * (1 - rate) + f.Alpha * rate;
			return new SKColor((byte)r, (byte)g, (byte)b, (byte)a);
		}
	}
}