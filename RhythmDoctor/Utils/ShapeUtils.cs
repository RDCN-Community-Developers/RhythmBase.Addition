using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Extensions;
using RhythmBase.RhythmDoctor.Events;
using SkiaSharp;

namespace RhythmBase.RhythmDoctor.Utils
{
	public static class ShapeUtils
	{
		public static void GenerateTriangleBitmap(string filename, int width, int height)
		{
			using SKBitmap bitmap = new(width, height);
			using SKCanvas canvas = new(bitmap);
			using SKPaint paint = new()
			{
				Color = SKColors.White,
				Style = SKPaintStyle.Fill
			};
			SKPath path = new();
			path.MoveTo(0, 0);
			path.LineTo(0, height);
			path.LineTo(width, height);
			path.Close();
			canvas.DrawPath(path, paint);
			bitmap.Save(filename);
		}
		public static void GenerateRectangleBitmap(string filename, int width, int height)
		{
			using SKBitmap bitmap = new(width, height);
			using SKCanvas canvas = new(bitmap);
			canvas.Clear(SKColors.White);
			bitmap.Save(filename);
		}
		public static Move[] GetMovesOfTriangles(RDRotatedRectN[] triangles, int width, int height)
		{
			Move[] moves = new Move[triangles.Length];
			for (int i = 0; i < triangles.Length; i++)
			{
				var (X, Y) = VisualUtils.PixelToPercent((triangles[i].Location.X, triangles[i].Location.Y));
				moves[i] = new Move()
				{
					Position = new RDPointN(X ?? 50, Y ?? 50),
					Pivot = default,
					Angle = triangles[i].Angle / float.Pi * 180f,
					Scale = new(triangles[i].Size.Width / width, triangles[i].Size.Height / height),
				};
			}
			return moves;
		}
	}
}
