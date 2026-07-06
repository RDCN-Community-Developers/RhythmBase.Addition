using RhythmBase.Global.Components.Vector;
using SkiaSharp;

namespace RhythmBase.Global.Utils
{
	public static class ShapeUtils
	{
		public static PointN[][] Earcut(PointN[] points, int[] holeIndices)
		{
			double[] points2 = [.. points.SelectMany(p => new double[] { p.X, p.Y })];
			int[] tessellation = [.. Utils.Earcut.Tessellate(points2, holeIndices)];
			PointN[][] result = new PointN[tessellation.Length / 3][];
			for (int i = 0; i < tessellation.Length; i += 3)
			{
				PointN[] triangle = new PointN[3];
				triangle[0] = points[tessellation[i]];
				triangle[1] = points[tessellation[i + 1]];
				triangle[2] = points[tessellation[i + 2]];
				result[i / 3] = triangle;
			}
			return result;
		}
		public static RotatedRectN[] CutToRightTriangle(PointN[] triangle, float tolerance = 0f)
		{
			PointN a = triangle[0];
			PointN b = triangle[1];
			PointN c = triangle[2];
			PointN o;
			PointN f;
			float ab2 = (a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y);
			float ac2 = (a.X - c.X) * (a.X - c.X) + (a.Y - c.Y) * (a.Y - c.Y);
			float bc2 = (b.X - c.X) * (b.X - c.X) + (b.Y - c.Y) * (b.Y - c.Y);
			if (ab2 < bc2 && ac2 < bc2)//a
			{
				if (ab2 + ac2 >= bc2 - tolerance && ab2 + ac2 <= bc2 + tolerance)
				{
					o = GetMiddle(b, c);
					return [GetTriangle(o, a, b)];
				}
				f = GetFooter(a, b, c);
				o = GetMiddle(a, b);
				RotatedRectN r1 = GetTriangle(o, f, a);
				o = GetMiddle(a, c);
				return [r1, GetTriangle(o, f, c)];
			}
			else if (ac2 < ab2 && bc2 < ab2)//c
			{
				if (ac2 + bc2 >= ab2 - tolerance && ac2 + bc2 <= ab2 + tolerance)
				{
					o = GetMiddle(a, b);
					return [GetTriangle(o, c, a)];
				}
				f = GetFooter(c, a, b);
				o = GetMiddle(c, b);
				RotatedRectN r1 = GetTriangle(o, f, b);
				o = GetMiddle(a, c);
				return [r1, GetTriangle(o, f, c)];
			}
			else//b
			{
				if (ab2 + bc2 >= ac2 - tolerance && ab2 + bc2 <= ac2 + tolerance)
				{
					o = GetMiddle(a, c);
					return [GetTriangle(o, b, c)];
				}
				f = GetFooter(b, a, c);
				o = GetMiddle(b, c);
				RotatedRectN r1 = GetTriangle(o, f, b);
				o = GetMiddle(a, b);
				return [r1, GetTriangle(o, f, a)];
			}
		}
		public static RotatedRectN[] CutToRightTriangles(PointN[][] triangles, float tolerance = 0f)
		{
			RotatedRectN[] result = [];
			foreach (PointN[] triangle in triangles)
			{
				result = [.. result, .. CutToRightTriangle(triangle, tolerance)];
			}
			return result;
		}
		public static RotatedRectN[] CutToRectangles(PointN[] polygon, SKCanvas canvas, float tolerance = 0f)
		{
			int l = polygon.Length;
			List<RotatedRectN> rectangles = [];
			if (l < 5)
			{
				return [];
			}
			for (int i = 0; i < l; i++)
			{
				PointN nearestP = default;
				float nearestDist = float.MaxValue;
				bool isFirstP = false;
				bool find = false;
				(PointN p1, PointN p2) line = (polygon[i], polygon[(i + 1) % l]);
				for (int j = i + 2; j < (i + l - 2); j++)
				{
					(PointN p1, PointN p2) nxtLine = (polygon[j % l], polygon[(j + 1) % l]);
					if (GetPerpendicularIntersectionPoint(line.p1, line.p2, nxtLine.p1, nxtLine.p2, out PointN p))
					{
						float dist = GetDistanceSquaredToSegment(p, line.p1, line.p2);
						if (float.Abs(dist) < float.Abs(nearestDist))
						{
							nearestDist = dist;
							nearestP = p;
							isFirstP = true;
							find = true;
						}
					}
					if (GetPerpendicularIntersectionPoint(line.p2, line.p1, nxtLine.p1, nxtLine.p2, out p))
					{
						float dist = GetDistanceSquaredToSegment(p, line.p1, line.p2);
						if (float.Abs(dist) < float.Abs(nearestDist))
						{
							nearestDist = dist;
							nearestP = p;
							isFirstP = false;
							find = true;
						}
					}
				}
				if (find)
				{
					if (isFirstP)
					{
						if (nearestDist > tolerance * tolerance)
						{
							PointN o = GetMiddle(line.p2, nearestP);
							rectangles.Add(GetTriangle(o, line.p1, nearestP));
						}
					}
					else
					{
						if (nearestDist > tolerance * tolerance)
						{
							PointN o = GetMiddle(line.p1, nearestP);
							rectangles.Add(GetTriangle(o, line.p2, line.p1));
						}
					}
					//canvas.DrawPoints(SKPointMode.Polygon,
					//[
					//	nearestP.ToSKPoint(),
					//	line.p1.ToSKPoint(),
					//	line.p2.ToSKPoint(),
					//	nearestP.ToSKPoint(),
					//], new SKPaint { Color = SKColors.Blue, Style = SKPaintStyle.Stroke, StrokeWidth = 1 });
				}
			}
			return [.. rectangles];
		}
		//public static void GenerateRectanglePrimitives(SKBitmap bitmap)
		//{
		//	RendererModel model = new(bitmap.Bytes, new() { });
		//}
		private static RotatedRectN GetTriangle(PointN center, PointN ocorner, PointN xcorner)
		{
			float degree = float.Atan2(xcorner.Y - ocorner.Y, xcorner.X - ocorner.X);
			float width2 = (ocorner.X - xcorner.X) * (ocorner.X - xcorner.X) + (ocorner.Y - xcorner.Y) * (ocorner.Y - xcorner.Y);
			float height2 = ((center.X - ocorner.X) * (center.X - ocorner.X) + (center.Y - ocorner.Y) * (center.Y - ocorner.Y)) * 4 - width2;
			float width = float.Sqrt(width2);
			float height = float.Sqrt(height2);
			return new RotatedRectN(center, new SizeN(width, height), new(width / 2, height / 2), degree);
		}
		private static PointN GetMiddle(PointN p1, PointN p2) => new((p1.X + p2.X) / 2, (p1.Y + p2.Y) / 2);
		private static PointN GetFooter(PointN p, PointN l1, PointN l2)
		{
			float dx = l2.X - l1.X;
			float dy = l2.Y - l1.Y;
			float d = float.Sqrt(dx * dx + dy * dy);
			if (d < 0.00001f) return p;
			dx /= d;
			dy /= d;
			float t = ((p.X - l1.X) * dx + (p.Y - l1.Y) * dy);
			return new PointN(l1.X + t * dx, l1.Y + t * dy);
		}
		public static bool GetPerpendicularIntersectionPoint(
				PointN l1f, PointN l2, PointN nl1, PointN nl2, out PointN result)
		{
			result = default;

			float vx = l2.X - l1f.X;
			float vy = l2.Y - l1f.Y;
			float wx = nl2.X - nl1.X;
			float wy = nl2.Y - nl1.Y;

			float dotWV = wx * vx + wy * vy;
			if (MathF.Abs(dotWV) < 1e-6f)
				return false; // 平行或重合，无解

			float dx = nl1.X - l1f.X;
			float dy = nl1.Y - l1f.Y;
			float dotDV = dx * vx + dy * vy;

			float t = -dotDV / dotWV;

			if (t < 0f || t > 1f)
				return false; // 交点不在线段 nl1-nl2 上

			result = new PointN(nl1.X + t * wx, nl1.Y + t * wy);
			return true;
		}
		private static float GetLengthSquared(PointN p1, PointN p2)
		{
			float dx = p1.X - p2.X;
			float dy = p1.Y - p2.Y;
			return dx * dx + dy * dy;
		}
		private static float GetDistanceSquaredToSegment(PointN p, PointN v, PointN w)
		{
			return GetLengthSquared(p, GetFooter(p, v, w));
		}
	}
}
