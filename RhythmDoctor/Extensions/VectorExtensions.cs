using RhythmBase.Global.Components.Vector;
using RhythmBase.RhythmDoctor.Assets;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using System.Runtime.CompilerServices;

namespace RhythmBase.RhythmDoctor.Extensions;

public static partial class Extensions
{
	public const int PixelateWidth = 352;
	public const int PixelateHeight = 198;
	extension(PointI p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointI ToPixelatedPoint() => (
			p.X is int x ? (int)(x / 100f * PixelateWidth) : null,
			p.Y is int y ? (int)(PixelateHeight - y / 100f * PixelateHeight) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointI ToPercentagePoint() => (
			p.X is int x ? (int)(x / (float)PixelateWidth * 100f) : null,
			p.Y is int y ? (int)(100f - y / (float)PixelateHeight * 100f) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointI ToPixelatedPoint(SizeNI pixelatedSize) => (
			p.X is int x ? (int)(x / 100f * pixelatedSize.Width) : null,
			p.Y is int y ? (int)(pixelatedSize.Height - y / 100f * pixelatedSize.Height) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointI ToPercentagePoint(SizeNI pixelatedSize) => (
			p.X is int x ? (int)(x / (float)pixelatedSize.Width * 100f) : null,
			p.Y is int y ? (int)(100f - y / (float)pixelatedSize.Height * 100f) : 0
		);
	}

	extension(Point p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point ToPixelatedPoint() => (
			p.X is float x ? (int)(x / 100f * PixelateWidth) : null,
			p.Y is float y ? (int)(PixelateHeight - y / 100f * PixelateHeight) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point ToPercentagePoint() => (
			p.X is float x ? (int)(x / PixelateWidth * 100f) : null,
			p.Y is float y ? (int)(100f - y / PixelateHeight * 100f) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point ToPixelatedPoint(SizeNI pixelatedSize) => (
			p.X is float x ? (int)(x / 100f * pixelatedSize.Width) : null,
			p.Y is float y ? (int)(pixelatedSize.Height - y / 100f * pixelatedSize.Height) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Point ToPercentagePoint(SizeNI pixelatedSize) => (
			p.X is float x ? (int)(x / pixelatedSize.Width * 100f) : null,
			p.Y is float y ? (int)(100f - y / pixelatedSize.Height * 100f) : 0
		);
	}

	extension(PointNI p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointNI ToPixelatedPoint() => (
			(int)(p.X / 100f * PixelateWidth),
			(int)(PixelateHeight - p.Y / 100f * PixelateHeight)
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointNI ToPercentagePoint() => (
			(int)(p.X / (float)PixelateWidth * 100f),
			(int)(100f - p.Y / (float)PixelateHeight * 100f)
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointNI ToPixelatedPoint(SizeNI pixelatedSize) => (
			(int)(p.X / 100f * pixelatedSize.Width),
			(int)(pixelatedSize.Height - p.Y / 100f * pixelatedSize.Height)
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointNI ToPercentagePoint(SizeNI pixelatedSize) => (
			(int)(p.X / (float)pixelatedSize.Width * 100f),
			(int)(100f - p.Y / (float)pixelatedSize.Height * 100f)
		);
	}

	extension(PointN p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointN ToPixelatedPoint() => (
			p.X / 100f * PixelateWidth,
			PixelateHeight - p.Y / 100f * PixelateHeight
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointN ToPercentagePoint() => (
			p.X / PixelateWidth * 100f,
			100f - p.Y / PixelateHeight * 100f
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointN ToPixelatedPoint(SizeNI pixelatedSize) => (
			p.X / 100f * pixelatedSize.Width,
			pixelatedSize.Height - p.Y / 100f * pixelatedSize.Height
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointN ToPercentagePoint(SizeNI pixelatedSize) => (
			p.X / pixelatedSize.Width * 100f,
			100f - p.Y / pixelatedSize.Height * 100f
		);
	}

	extension(PointE p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointE ToPixelatedPoint() => (
			p.X / 100f * PixelateWidth,
			PixelateHeight - p.Y / 100f * PixelateHeight
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointE ToPercentagePoint() => (
			p.X / PixelateWidth * 100f,
			100f - p.Y / PixelateHeight * 100f
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointE ToPixelatedPoint(SizeNI pixelatedSize) => (
			p.X / 100f * pixelatedSize.Width,
			pixelatedSize.Height - p.Y / 100f * pixelatedSize.Height
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public PointE ToPercentagePoint(SizeNI pixelatedSize) => (
			p.X / pixelatedSize.Width * 100f,
			100f - p.Y / pixelatedSize.Height * 100f
		);
	}
	extension(SizeI p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeI ToPixelatedPoint() => (
			p.Width is int x ? (int)(x / 100f * PixelateWidth) : null,
			p.Height is int y ? (int)(PixelateHeight - y / 100f * PixelateHeight) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeI ToPercentagePoint() => (
			p.Width is int x ? (int)(x / (float)PixelateWidth * 100f) : null,
			p.Height is int y ? (int)(100f - y / (float)PixelateHeight * 100f) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeI ToPixelatedPoint(SizeNI pixelatedSize) => (
			p.Width is int x ? (int)(x / 100f * pixelatedSize.Width) : null,
			p.Height is int y ? (int)(pixelatedSize.Height - y / 100f * pixelatedSize.Height) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeI ToPercentagePoint(SizeNI pixelatedSize) => (
			p.Width is int x ? (int)(x / (float)pixelatedSize.Width * 100f) : null,
			p.Height is int y ? (int)(100f - y / (float)pixelatedSize.Height * 100f) : 0
		);
	}

	extension(Size p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Size ToPixelatedPoint() => (
			p.Width is float x ? (int)(x / 100f * PixelateWidth) : null,
			p.Height is float y ? (int)(PixelateHeight - y / 100f * PixelateHeight) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Size ToPercentagePoint() => (
			p.Width is float x ? (int)(x / PixelateWidth * 100f) : null,
			p.Height is float y ? (int)(100f - y / PixelateHeight * 100f) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Size ToPixelatedPoint(SizeNI pixelatedSize) => (
			p.Width is float x ? (int)(x / 100f * pixelatedSize.Width) : null,
			p.Height is float y ? (int)(pixelatedSize.Height - y / 100f * pixelatedSize.Height) : 0
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public Size ToPercentagePoint(SizeNI pixelatedSize) => (
			p.Width is float x ? (int)(x / pixelatedSize.Width * 100f) : null,
			p.Height is float y ? (int)(100f - y / pixelatedSize.Height * 100f) : 0
		);
	}

	extension(SizeNI p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeNI ToPixelatedPoint() => (
			(int)(p.Width / 100f * PixelateWidth),
			(int)(PixelateHeight - p.Height / 100f * PixelateHeight)
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeNI ToPercentagePoint() => (
			(int)(p.Width / (float)PixelateWidth * 100f),
			(int)(100f - p.Height / (float)PixelateHeight * 100f)
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeNI ToPixelatedPoint(SizeNI pixelatedSize) => (
			(int)(p.Width / 100f * pixelatedSize.Width),
			(int)(pixelatedSize.Height - p.Height / 100f * pixelatedSize.Height)
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeNI ToPercentagePoint(SizeNI pixelatedSize) => (
			(int)(p.Width / (float)pixelatedSize.Width * 100f),
			(int)(100f - p.Height / (float)pixelatedSize.Height * 100f)
		);
	}

	extension(SizeN p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeN ToPixelatedPoint() => (
			p.Width / 100f * PixelateWidth,
			PixelateHeight - p.Height / 100f * PixelateHeight
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeN ToPercentagePoint() => (
			p.Width / PixelateWidth * 100f,
			100f - p.Height / PixelateHeight * 100f
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeN ToPixelatedPoint(SizeNI pixelatedSize) => (
			p.Width / 100f * pixelatedSize.Width,
			pixelatedSize.Height - p.Height / 100f * pixelatedSize.Height
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeN ToPercentagePoint(SizeNI pixelatedSize) => (
			p.Width / pixelatedSize.Width * 100f,
			100f - p.Height / pixelatedSize.Height * 100f
		);
	}

	extension(SizeE p)
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeE ToPixelatedPoint() => (
			p.Width / 100f * PixelateWidth,
			PixelateHeight - p.Height / 100f * PixelateHeight
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeE ToPercentagePoint() => (
			p.Width / PixelateWidth * 100f,
			100f - p.Height / PixelateHeight * 100f
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeE ToPixelatedPoint(SizeNI pixelatedSize) => (
			p.Width / 100f * pixelatedSize.Width,
			pixelatedSize.Height - p.Height / 100f * pixelatedSize.Height
		);
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public SizeE ToPercentagePoint(SizeNI pixelatedSize) => (
			p.Width / pixelatedSize.Width * 100f,
			100f - p.Height / pixelatedSize.Height * 100f
		);
	}

	private static Dictionary<Decoration, SizeNI> _decoSizeCache = [];
	private static Dictionary<Row, SizeNI> _rowSizeCache = [];
	extension(Decoration e)
	{
		public Sprite? Sprite
		{
			get => Level.Default.AssetManager.GetFile<Sprite>(e.Character.StringName ?? "");
		}
		public SizeNI Size
		{
			get
			{
				if (_decoSizeCache.TryGetValue(e, out SizeNI size))
					return size;
				return _decoSizeCache[e] = e.Sprite?.Size ?? default;
			}
		}
	}
	extension(Row e)
	{
		public Sprite? Sprite
		{
			get => Level.Default.AssetManager.GetFile<Sprite>(e.Character.StringName ?? "");
		}
		public SizeNI Size
		{
			get
			{
				if (_rowSizeCache.TryGetValue(e, out SizeNI size))
					return size;
				return _rowSizeCache[e] = e.Sprite?.Size ?? default;
			}
		}
	}

	extension(Move e)
	{
		public PointE? PPosition
		{
			get => e.Position?.ToPixelatedPoint();
			set => e.Position = value?.ToPercentagePoint();
		}
		public Point? PPivot
		{
			get => e.Pivot?.ToPixelatedPoint(e.Parent?.Size ?? default);
			set => e.Pivot = value?.ToPercentagePoint(e.Parent?.Size ?? default);
		}
		public SizeE? PScale
		{
			get => e.Scale?.ToPixelatedPoint(e.Parent?.Size ?? default);
			set => e.Scale = value?.ToPercentagePoint(e.Parent?.Size ?? default);
		}
	}
	extension(MoveRoom e)
	{
		public Point? PPosition
		{
			get => e.Position?.ToPixelatedPoint();
			set => e.Position = value?.ToPercentagePoint();
		}
		public Point? PPivot
		{
			get => e.Pivot?.ToPixelatedPoint();
			set => e.Pivot = value?.ToPercentagePoint();
		}
		public Size? PScale
		{
			get => e.Scale?.ToPixelatedPoint();
			set => e.Scale = value?.ToPercentagePoint();
		}
	}
	extension(MoveRow e)
	{
		public PointE? PPosition
		{
			get => e.Position?.ToPixelatedPoint();
			set => e.Position = value?.ToPercentagePoint();
		}
		public SizeE? PScale
		{
			get => e.Scale?.ToPixelatedPoint(e.Parent?.Size ?? default);
			set => e.Scale = value?.ToPercentagePoint(e.Parent?.Size ?? default);
		}
	}
}