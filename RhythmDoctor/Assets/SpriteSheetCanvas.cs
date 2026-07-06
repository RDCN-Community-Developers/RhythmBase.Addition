using RhythmBase.Global.Components;
using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Extensions;
using RhythmBase.RhythmDoctor.Assets.FlexibleSprite;
using SkiaSharp;

namespace RhythmBase.RhythmDoctor.Assets
{
	public struct SpriteSheetCanvasSettings()
	{
		public OutputDirection Direction { get; set; } = OutputDirection.Horizontal;
		public int MaxWidth { get; set; } = 1024;
		public int MaxHeight { get; set; } = 1024;
		public float GlowSigma { get; set; } = 5;
		public Color GlowColor { get; set; } = Color.White;
		public float StrokeWidth { get; set; } = 1;
		public Color StrokeColor { get; set; } = Color.White;
		public RectNI Margin { get; set; } = new();
		public SKPaint DefaultEffect { get; set; } = new()
		{
			Color = SKColors.Black,
		};
	}
	public class SpriteSheetCanvas(
		OutputDirection direction,
		int maxWidth = 1024,
		int maxHeight = 1024)
	{
		private readonly OutputDirection direction = direction;
		private readonly int width = maxWidth;
		private readonly int height = maxHeight;
		private int drawIndex = 0;
		private int curPage = 0;

		private readonly List<Frame> frameToDraw = [];
		private SKRectI currentMaxRect = new();
		private readonly SpriteSheetBook book = new();
		public float DefaultGlowSigma { get; set; } = 5;
		public Color DefaultGlowColor { get; set; } = Color.White;
		public float DefaultStrokeWidth { get; set; } = 1;
		public Color DefaultOutlineColor { get; set; } = Color.White;
		public SKPaint DefaultEffect { get; set; } = new()
		{
			Color = SKColors.Black,
		};
		public RectNI Margin { get; set; } = new();
		public int CurrentPage => curPage;
		public SpriteSheetCanvas(OutputDirection direction, SKSizeI maxSize) : this(direction, maxSize.Width, maxSize.Height) { }
		public SpriteSheetCanvas(OutputDirection direction) : this(direction, int.MaxValue, int.MaxValue) { }
		public SpriteSheetCanvas(SpriteSheetCanvasSettings settings) : this(settings.Direction, settings.MaxWidth, settings.MaxHeight)
		{
			DefaultGlowSigma = settings.GlowSigma;
			DefaultGlowColor = settings.GlowColor;
			DefaultStrokeWidth = settings.StrokeWidth;
			DefaultOutlineColor = settings.StrokeColor;
			DefaultEffect = settings.DefaultEffect?.Clone() ?? new SKPaint()
			{
				Color = SKColors.Black,
			};
			Margin = settings.Margin;
		}
		public void DrawBitmaps(IEnumerable<SKBitmap> imgs)
		{
			foreach (SKBitmap img in imgs)
				DrawBitmap(img);
		}
		public void DrawBitmap(SKBitmap img) => DrawFrame(img);
		public SKRect[] DrawTexts(string[] texts, SKFont font, out SKPoint[] positions, out float[] widths)
		{
			List<SKRect> _rects = [];
			List<SKPoint> _positions = [];
			List<float> _widths = [];
			foreach (string text in texts)
			{
				_rects.Add(DrawText(text, font, out SKPoint position, out float width));
				_positions.Add(position);
				_widths.Add(width);
			}
			positions = [.. _positions];
			widths = [.. _widths];
			return [.. _rects];
		}
		public SKRect DrawText(string text, SKFont font, out SKPoint position, out float width)
		{
			float _glowOutlineWidth = float.Max(DefaultGlowSigma * 3, DefaultStrokeWidth);
			width = font.MeasureText(text, out SKRect rect, DefaultEffect);
			SKPoint off = new(-rect.Left, -rect.Top);
			off.Offset(_glowOutlineWidth, _glowOutlineWidth);
			rect.Offset(off.X, off.Y);
			SKBitmap _base = new((int)(rect.Width + _glowOutlineWidth * 2), (int)(rect.Height + _glowOutlineWidth * 2));
			SKBitmap _glow = _base.Copy();
			SKBitmap _outline = _base.Copy();
			using (SKCanvas _baseCanvas = new(_base))
			{
				_baseCanvas.DrawText(text, off, font, DefaultEffect);
#if DEBUG
				_baseCanvas.DrawRect(rect, new() { Style = SKPaintStyle.Stroke, Color = SKColors.Red, StrokeWidth = 1 });
				_baseCanvas.DrawLine(rect.Right, rect.Top, off.X, off.Y, new() { Color = SKColors.Green, StrokeWidth = 1 });
				_baseCanvas.DrawLine(rect.Right, rect.Bottom, off.X, off.Y, new() { Color = SKColors.Green, StrokeWidth = 1 });
				_baseCanvas.DrawLine(off.X, off.Y, off.X + width, off.Y, new() { Color = SKColors.Blue, StrokeWidth = 1 });
#endif
			}
			using (SKCanvas _glowCanvas = new(_glow))
			{
				using SKPaint _glowPaint = DefaultEffect?.Clone() ?? new SKPaint();
				_glowPaint.Color = DefaultGlowColor.ToSKColor();
				_glowPaint.MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Outer, DefaultGlowSigma);
				_glowCanvas.DrawText(text, off, font, _glowPaint);
			}
			using (SKCanvas _outlineCanvas = new(_outline))
			{
				using SKPaint _outlinePaint = DefaultEffect?.Clone() ?? new SKPaint();
				_outlinePaint.Color = DefaultOutlineColor.ToSKColor();
				_outlinePaint.Style = SKPaintStyle.Stroke;
				_outlinePaint.StrokeWidth = DefaultStrokeWidth;
				_outlineCanvas.DrawText(text, off, font, _outlinePaint);
			}
			DrawFrame(new(_base, _glow, _outline) { Name = text });
			position = off;
			return rect;
		}
		public void DrawFrames(IEnumerable<Frame> frames)
		{
			foreach (Frame img in frames)
				DrawFrame(img);
		}
		public void DrawFrame(Frame img)
		{
			SKRectI measureRect = img.Base.Info.Rect;
			bool lessFit = CanFit(currentMaxRect.Size, out GridCoordinate imageSize);
			bool moreFit = CanFit(SKRectI.Union(currentMaxRect, measureRect).Size, out var _, true);
			if (!lessFit)
				throw new NotSupportedException($"Image size too big.");
			else if (!moreFit)
			{
				Flush(imageSize, currentMaxRect);
				currentMaxRect = new();
			}
			currentMaxRect.Union(measureRect);
			frameToDraw.Add(img);
		}
		private bool CanFit(SKSizeI maxTextSize, out GridCoordinate minCardGrid, bool more = false)
		{
			if (maxTextSize.IsEmpty)
			{
				minCardGrid = new(0, 0);
				return true;
			}
			GridCoordinate maxStruct = RowColumnSize(AddMargin(maxTextSize, Margin.ToSKRect()));
			bool can = frameToDraw.Count + (more ? 1 : 0) <= maxStruct.Count;
			switch (direction)
			{
				case OutputDirection.Horizontal:
					minCardGrid = new(
						(int)Math.Ceiling(frameToDraw.Count / (float)maxStruct.ColumnCount),
						Math.Min(frameToDraw.Count, maxStruct.ColumnCount));
					break;
				case OutputDirection.Vertical:
					minCardGrid = new(
						Math.Min(frameToDraw.Count, maxStruct.RowCount),
						(int)Math.Ceiling(frameToDraw.Count / (float)maxStruct.RowCount));
					break;
				case OutputDirection.PackedHorizontal or OutputDirection.PackedVertical:
					int r = 1;
					int c = 1;
					do
					{
						if (r * maxTextSize.Width < c * maxTextSize.Height) r += 1;
						else c += 1;
					} while (r * c < maxStruct.Count);
					minCardGrid = new(r, c);
					break;
				default:
					throw new NotImplementedException();
			}
			return can;
		}
		GridCoordinate RowColumnSize(SKSize maxTextSize)
		{
			int maxRow = (int)Math.Floor(height / maxTextSize.Height);
			int maxCol = (int)Math.Floor(width / maxTextSize.Width);
			return new(maxRow, maxCol);
		}
		private void Flush(GridCoordinate imageStruct, SKRectI contentSize)
		{
			if (imageStruct.Count == 0)
				throw new NotSupportedException($"Image size too big, at \"{frameToDraw[0]}\".");
			using SKPaint paintGlow = DefaultEffect?.Clone() ?? new();
			paintGlow.Color = DefaultGlowColor.ToSKColor();
			paintGlow.ImageFilter = SKImageFilter.CreateDropShadowOnly(
					0, 0, DefaultGlowSigma, DefaultGlowSigma, DefaultGlowColor.ToSKColor()
				);
			using SKPaint paintOutline = DefaultEffect?.Clone() ?? new();
			paintOutline.ImageFilter = SKImageFilter.CreateCompose(
					SKImageFilter.CreateMatrixConvolution(
						new(3, 3),
						[
							-1, -1, -1,
							-1, 8, -1,
							-1, -1, -1
						],
						1f, 0f, new(1, 1), SKShaderTileMode.Clamp, false
						),
					SKImageFilter.CreateColorFilter(SKColorFilter.CreateColorMatrix(
						[
							0, 0, 0, 0, 255,
							0, 0, 0, 0, 255,
							0, 0, 0, 0, 255,
							0, 0, 0, 1, 255
						]
						)));
			SKSizeI cardSize = AddMargin(contentSize.Size, Margin.ToSKRect());
			SKBitmap bitmap = new(imageStruct.ColumnCount * cardSize.Width, imageStruct.RowCount * cardSize.Height);
			SKBitmap bitmapGlow = new(imageStruct.ColumnCount * cardSize.Width, imageStruct.RowCount * cardSize.Height);
			SKBitmap bitmapOutline = new(imageStruct.ColumnCount * cardSize.Width, imageStruct.RowCount * cardSize.Height);
			using SKCanvas canvas = new(bitmap);
			using SKCanvas canvasGlow = new(bitmapGlow);
			using SKCanvas canvasOutline = new(bitmapOutline);
			int curIndex = 0;
			do
			{
				SKRect bound = contentSize;
				bound.Offset(Margin.Left, Margin.Top);
				bound.Offset(0, -contentSize.Top);
				bound.Offset(direction switch
				{
					OutputDirection.Horizontal or OutputDirection.PackedHorizontal => new(
						curIndex % imageStruct.ColumnCount * cardSize.Width,
						curIndex / imageStruct.ColumnCount * cardSize.Height),
					OutputDirection.Vertical or OutputDirection.PackedVertical => new(
						curIndex / imageStruct.RowCount * cardSize.Width,
						curIndex % imageStruct.RowCount * cardSize.Height),
					_ => throw new NotImplementedException(),
				});
#if DEBUG
				canvas.DrawRect(bound, new() { Style = SKPaintStyle.Fill, Color = SKColors.White.WithAlpha(10), StrokeWidth = 1 });
#endif
				bound.Size = frameToDraw[0].Size;
				canvas.DrawBitmap(frameToDraw[0].Base, bound, null);
				if (frameToDraw[0].Glow is null)
					canvasGlow.DrawBitmap(frameToDraw[0].Base, bound, paintGlow);
				else
					canvasGlow.DrawBitmap(frameToDraw[0].Glow, bound, null);
				if (frameToDraw[0].Outline is null)
					canvasOutline.DrawBitmap(frameToDraw[0].Base, bound, paintOutline);
				else
					canvasOutline.DrawBitmap(frameToDraw[0].Outline, bound, null);
				int cardIndex = direction switch
				{
					OutputDirection.Horizontal or OutputDirection.PackedHorizontal => curIndex,
					OutputDirection.Vertical or OutputDirection.PackedVertical => (curIndex % imageStruct.RowCount) * imageStruct.ColumnCount + (curIndex / imageStruct.RowCount),
					_ => throw new NotImplementedException(),
				};
				book.CardIndice.Add(new(book.PageCount, drawIndex, cardIndex) { Name = frameToDraw[0].Name });
				frameToDraw.RemoveAt(0);
				curIndex++;
				drawIndex++;
			} while (frameToDraw.Count > 0);
			book.PageInfos.Add(new(new(imageStruct.RowCount, imageStruct.ColumnCount), cardSize)
			{
				image = bitmap,
				imageGlow = bitmapGlow,
				imageOutline = bitmapOutline,
			});
			book.PageCount++;
			curPage++;
		}
		public SpriteSheetBook Build(out RDSprite[] sprites)
		{
			bool canHold = CanFit(currentMaxRect.Size, out GridCoordinate s);
			if (!canHold)
				throw new NotSupportedException($"Image size too big.");
			Flush(s, currentMaxRect);
			List<RDSprite> _sprites = [];
			foreach (var page in book.PageInfos)
			{
				RDSprite sprite = new()
				{
					ImageBase = page.image,
					ImageGlow = page.imageGlow,
					ImageOutline = page.imageOutline,
					Size = page.cardSize.ToRDSize(),
				};
				PageIndex[] frames = [.. book.CardIndice.Where(j => j.Page == book.PageInfos.IndexOf(page))];
				for (int i = 0; i < frames.Length; i++)
				{
					PageIndex frame = frames[i];
					if (string.IsNullOrEmpty(frame.Name))
						frame.Name = $"{frame.ImageIndex}";
					frames[i] = frame;
					sprite.Clips.Add(new RDSprite.Expression()
					{
						Name = frame.Name,
						Frames = [frame.FrameIndex],
					});
				}
				_sprites.Add(sprite);
			}
			sprites = [.. _sprites];
			return book;
		}
		private static SKSizeI AddMargin(SKSizeI size, SKRectI margin) => new(
				size.Width + margin.Left + margin.Right,
				size.Height + margin.Top + margin.Bottom);
	}
}
