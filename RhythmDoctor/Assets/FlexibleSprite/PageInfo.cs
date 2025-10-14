using SkiaSharp;

namespace RhythmBase.RhythmDoctor.Assets.FlexibleSprite
{
	public struct PageInfo(GridCoordinate grid, SKSizeI card)
	{
		public GridCoordinate grid = grid;
		public SKSizeI cardSize = card;
		public required SKBitmap image;
		public required SKBitmap imageGlow;
		public required SKBitmap imageOutline;
	}
}
