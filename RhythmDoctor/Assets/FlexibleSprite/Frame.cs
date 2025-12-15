using SkiaSharp;

namespace RhythmBase.RhythmDoctor.Assets.FlexibleSprite
{
	public class Frame
	{
		private SKSizeI _size;
		public string Name { get; set; } = string.Empty;
		public SKSizeI Size
		{
			get => _size;
			set
			{
				SKSizeI maxSize = GetMaxSize();
				if (value.Width < maxSize.Width || value.Height < maxSize.Height)
					return;
				_size = value;
			}
		}
		public SKBitmap Base { get; init; }
		public SKBitmap? Glow { get; init; }
		public SKBitmap? Outline { get; init; }
		public Frame(SKBitmap @base)
		{
			Base = @base;
			_size = @base.Info.Size;
		}
		public Frame(SKBitmap @base, SKBitmap? glow = null, SKBitmap? outline = null)
		{
			Base = @base;
			Glow = glow;
			Outline = outline;
			_size = GetMaxSize();
		}
		private SKSizeI GetMaxSize()
		{
			SKSizeI b = Base.Info.Size;
			SKSizeI g = Glow?.Info.Size ?? SKSizeI.Empty;
			SKSizeI o = Outline?.Info.Size ?? SKSizeI.Empty;
			return new SKSizeI(
				Math.Max(b.Width, Math.Max(g.Width, o.Width)),
				Math.Max(b.Height, Math.Max(g.Height, o.Height))
			);
		}
		public static implicit operator Frame(SKBitmap bitmap) => new(bitmap);
	}
}
