using RhythmBase.Global.Extensions;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RhythmBase.Global.Utils
{
    internal static class OtherUtils
    {

		internal static void OutputData1DBitmap(this float[] data, string fileName, int size, SKColor foreground, SKColor background) => OutputData1DBitmap(data.Select(i => (double)i).ToArray(), fileName, size, foreground, background);
		internal static void OutputData1DBitmap(this double[] data, string fileName, int size, SKColor foreground, SKColor background)
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
		internal static void OutputData2DBitmap(this float[,] data, string filename, SKColor foreground, SKColor background)
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
		internal static void OutputData2DBitmap(this double[,] data, string filename, SKColor foreground, SKColor background)
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
		internal static void OutputData2DBitmap(this float[][] data, string filename, SKColor foreground, SKColor background) => data.Select(i => i.Select(j => (double)j).ToArray()).ToArray().OutputData2DBitmap(filename, foreground, background);
		internal static void OutputData2DBitmap(this double[][] data, string filename, SKColor foreground, SKColor background)
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
