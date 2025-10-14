using SkiaSharp;

namespace RhythmBase.RhythmDoctor.Assets.FlexibleSprite
{
	public record struct GridCoordinate(int RowCount, int ColumnCount)
	{
		public readonly int Count => RowCount * ColumnCount;
		public static SKPoint operator *(GridCoordinate left, SKPoint right) => new(left.ColumnCount * right.X, left.RowCount * right.Y);
		public static SKPoint operator *(SKPoint left, GridCoordinate right) => right * left;
		public static SKPointI operator *(GridCoordinate left, SKPointI right) => new(left.ColumnCount * right.X, left.RowCount * right.Y);
		public static SKPointI operator *(SKPointI left, GridCoordinate right) => right * left;
		public static SKSize operator *(GridCoordinate left, SKSize right) => new(left.ColumnCount * right.Width, left.RowCount * right.Height);
		public static SKSize operator *(SKSize left, GridCoordinate right) => right * left;
		public static SKSizeI operator *(GridCoordinate left, SKSizeI right) => new(left.ColumnCount * right.Width, left.RowCount * right.Height);
		public static SKSizeI operator *(SKSizeI left, GridCoordinate right) => right * left;
	}
}
