using RhythmBase.Global.Components;
using RhythmBase.RhythmDoctor.Assets;
using RhythmBase.RhythmDoctor.Assets.FlexibleSprite;
using SkiaSharp;

namespace RhythmBase.RhythmDoctor.Utils
{
	public class SpriteTextBuilder
	{
		private readonly Dictionary<char, float> widths = [];
		private readonly Dictionary<char, SKPoint> positions = [];
		private readonly Dictionary<char, int> pages = [];
		private readonly string filename;
		public RDSprite[] Sprites { get; }
		public string SpriteNameOf(char c) => pages.TryGetValue(c, out int value) ? $"{Path.GetFileNameWithoutExtension(filename)}_{value}" : string.Empty;
		public RDPointN[] GetPositions(string text, float scale = 1f)
		{
			List<RDPointN> ps = [];
			float width = 0;
			char[] chars = [.. text];
			for (int i = 0; i < chars.Length; i++)
			{
				if (positions.TryGetValue(chars[i], out SKPoint pos))
				{
					(float x, float y) p = (width * scale, pos.Y * scale);
					var (X, Y) = VisualUtils.PixelToPercent(p);
					ps.Add(new(X ?? 0, Y ?? 0));
					width += widths[chars[i]];
				}
			}
			return [.. ps];
		}
		public SpriteTextBuilder(SKFont font, string filename, string charCollection, SpriteSheetCanvasSettings settings)
		{
			this.filename = filename;
			RDSprite[] sprites = BuildCharsSprite(charCollection, font, settings);

			Sprites = sprites;

			for (int i = 0; i < sprites.Length; i++)
			{
				RDSprite sprite = sprites[i];
				sprite.Name = filename;
				sprite.Save(filename + "_" + i);
			}
		}
		private RDSprite[] BuildCharsSprite(string text, SKFont font, SpriteSheetCanvasSettings settings)
		{
			SpriteSheetCanvas canvas = new(settings);
			char[] chars = [.. text.ToCharArray().Distinct()];
			foreach (char c in chars)
			{
				var rect = canvas.DrawText(c.ToString(), font, out var pos, out var width);
				pages[c] = canvas.CurrentPage;
				widths[c] = width;
				positions[c] = pos;
			}
			SpriteSheetBook book = canvas.Build(out RDSprite[] sprites);
			for (int i = 0; i < chars.Length; i++)
			{
				PageIndex index = book.CardIndice.Single(j => j.ImageIndex == i);
				sprites[index.Page].Clips.Add(new()
				{
					Name = chars[i].ToString(),
					Frames = [index.FrameIndex],
				});
			}
			for (int i = 0; i < sprites.Length; i++)
			{
				RDSprite sprite = sprites[i];
				sprite.Clips.Add(new()
				{
					Name = "neutral",
					Frames = [],
				});
			}
			return sprites;
		}
	}
}