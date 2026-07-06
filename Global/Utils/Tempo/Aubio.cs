namespace RhythmBase.Global.Utils.Tempo;

public enum WindowType
{
	Rectangle,
	Hamming,
	Hanning,
	Hanningz,
	Blackman,
	BlackmanHarris,
	Gaussian,
	Welch,
	Parzen,
	Default = Hanningz,
}
internal static class Aubio
{
	const float _2pi = 2 * float.Pi;
	private static void SetWindow(float[] win, WindowType type)
	{
		float[] w = win;
		int i, size = win.Length;
		switch (type)
		{
			case WindowType.Rectangle:
				for (i = 0; i < size; i++) w[i] = 0.5f;
				break;
			case WindowType.Hamming:
				for (i = 0; i < size; i++) w[i] = 0.54f - (0.46f * float.Cos(_2pi * i / size));
				break;
			case WindowType.Hanning:
				for (i = 0; i < size; i++) w[i] = 0.5f - (0.5f * float.Cos(_2pi * i / size));
				break;
			case WindowType.Hanningz:
				for (i = 0; i < size; i++) w[i] = 0.5f * (1.0f - float.Cos(_2pi * i / size));
				break;
			case WindowType.Blackman:
				for (i = 0; i < size; i++) w[i] = 0.42f - (0.5f * float.Cos(_2pi * i / (size - 1f))) + (0.08f * float.Cos(4f * float.Pi * i / (size - 1f)));
				break;
			case WindowType.BlackmanHarris:
				for (i = 0; i < size; i++) w[i] = 0.35875f - (0.48829f * float.Cos(_2pi * i / (size - 1f))) + (0.14128f * float.Cos(2 * _2pi * i / (size - 1f))) - (0.01168f * float.Cos(3 * _2pi * i / (size - 1f)));
				break;
			case WindowType.Gaussian:
				double a, b, c = 0.5;
				for (int n = 0; n < size; n++)
				{
					a = (n - c * (size - 1)) / ((c * c) * (size - 1f));
					b = -c * (a * a);
					w[n] = (float)double.Exp(b);
				}
				break;
			case WindowType.Welch:
				for (i = 0; i < size; i++) w[i] = 1 - float.Pow((2 * i - size) / (size + 1), 2);
				break;
			case WindowType.Parzen:
				for (i = 0; i < size; i++) w[i] = 1 - float.Abs((2 * i - size) / (size + 1));
				break;
			default:
				break;
		}
	}
	internal static float[] CreateWindow(WindowType type, int length)
	{
		float[] win = new float[length];
		SetWindow(win, type);
		return win;
	}
	internal static double[] CreateWindowDouble(WindowType type, int length)
	{
		float[] fwin = CreateWindow(type, length);
		double[] dwin = new double[length];
		for (int i = 0; i < length; i++)
			dwin[i] = fwin[i];
		return dwin;
	}
	internal static float Median(float[] input)
	{
		int n = input.Length;
		int low, high;
		int median;
		int middle, ll, hh;

		int numOuter = 0, numInner = 0;

		low = 0;
		high = n - 1;
		median = (low + high) / 2;
		for (; ; )
		{
			++numOuter;

			if (high <= low)
				return input[median];
			if (high == low + 1)
			{
				if (input[low] > input[high])
					(input[low], input[high]) = (input[high], input[low]);
				return input[median];
			}

			middle = (low + high) / 2;
			if (input[middle] > input[high]) (input[middle], input[high]) = (input[high], input[middle]);
			if (input[low] > input[high]) (input[low], input[high]) = (input[high], input[low]);
			if (input[middle] > input[low]) (input[middle], input[low]) = (input[low], input[middle]);

			(input[middle], input[low + 1]) = (input[low + 1], input[middle]);

			ll = low + 1;
			hh = high;
			for (; ; )
			{
				do ll++; while (input[low] > input[ll]);
				do hh--; while (input[hh] > input[low]);

				if (hh < ll)
					break;
				++numInner;
				(input[ll], input[hh]) = (input[hh], input[ll]);
			}
			(input[low], input[hh]) = (input[hh], input[low]);
			if (hh <= median)
				low = ll;
			if (hh >= median)
				high = hh - 1;
		}
	}
	internal static bool PeakPick(float[] onset, int pos) =>
			onset[pos] > onset[pos - 1] &&
			onset[pos] > onset[pos + 1] &&
			onset[pos] > 0;
	internal static float UnwrapToPi(float phase) => phase + _2pi * (1 + float.Floor(-(phase + float.Pi) / _2pi));
	internal static void Shift(float[] s)
	{
		for (int j = 0; j < s.Length / 2; ++j)
			(s[j], s[j + s.Length / 2]) = (s[j + s.Length / 2], s[j]);
	}
	internal static float QuadraticPeakPos(float[] x, int pos)
	{
		float s0, s1, s2;
		int x0, x2;
		if (pos == 0 || pos == x.Length - 1) return pos;
		x0 = (pos < 1) ? pos : pos - 1;
		x2 = (pos + 1 < x.Length) ? pos + 1 : pos;
		if (x0 == pos) return (x[pos] <= x[x2]) ? pos : x2;
		if (x2 == pos) return (x[pos] <= x[x0]) ? pos : x0;
		s0 = x[x0];
		s1 = x[pos];
		s2 = x[x2];
		return pos + 0.5f * (s0 - s2) / (s0 - 2f * s1 + s2);
	}
	private static float DbSpl(float[] o) => 10 * float.Log10(o.Average(i => i * i));
	internal static bool SilenceDetection(float[] o, float threshold) => DbSpl(o) < threshold;
}