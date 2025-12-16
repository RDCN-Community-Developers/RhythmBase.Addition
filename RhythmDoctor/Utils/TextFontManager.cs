using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Extensions;
using RhythmBase.RhythmDoctor.Assets;
using RhythmBase.RhythmDoctor.Assets.FlexibleSprite;
using RhythmBase.RhythmDoctor.Components;
using SkiaSharp;

namespace RhythmBase.RhythmDoctor.Utils
{
	public record struct TextInfo(string Character, RDPointN Position, RDRectN Bound, float Width, RDSizeNI Size)
	{
		public RDPointN DecorationPivot => new(
						Position.X / Size.Width * 100,
						(1 - Position.Y / Size.Height) * 100);
	}
	public class TextFontManager
	{
		private readonly Dictionary<string, TextInfo> infos = [];
		private readonly string filename;
		private readonly SKFont font;
		private readonly SpriteSheetCanvas spriteSheetCanvas;
		private bool isBuilt { get; set; } = false;
		private SpriteSheetBook? builtBook = null;
		private DecorationPool<string>[]? pools;
		public TextFontManager AddWord(string word)
		{
			if (infos.TryGetValue(word, out _))
				return this;
			var rect = spriteSheetCanvas.DrawText(word, font, out var pos, out var width);
			TextInfo newInfo = new(word, pos.ToRDPoint(), rect.ToRDRect(), width, default);
			infos[word] = newInfo;
			return this;
		}
		public TextFontManager AddWords(IEnumerable<string> words)
		{
			foreach (var word in words)
				AddWord(word);
			return this;
		}
		public TextFontManager(SKFont font, string filename, SpriteSheetCanvasSettings settings)
		{
			this.font = font;
			this.filename = filename;
			spriteSheetCanvas = new SpriteSheetCanvas(settings);
		}
		public void Build(RDLevel level, out RDSprite[] sprites, int maxPoolSize = 1000)
		{
			builtBook = spriteSheetCanvas.Build(out RDSprite[] result);
			sprites = [.. result.Select((s, i)=>
						{
								s.Name = $"{filename}-{i}";
								foreach(var clip in s.Clips)
										clip.Loop = LoopOption.onBeat;
								s.AddBlankExpressionForDecoration();
								return s;
						})];
			pools = new DecorationPool<string>[builtBook.PageCount];
			for (int i = 0; i < pools.Length; i++)
				pools[i] = new DecorationPool<string>(level, $"{filename}-{i}", maxPoolSize);
			isBuilt = true;
		}
		public (Decoration, TextInfo) Allocate(float start, float end, string word)
		{
			if (!isBuilt)
				throw new InvalidOperationException("You must call Build() before Allocate().");
			PageIndex? page = builtBook?.CardIndice?
					.First(i => i.Name == word);
			if (page is PageIndex notnull)
				return (
						pools![notnull.Page].Allocate(start, end, word),
						infos[word] with
						{
							Size =
										builtBook!.PageInfos[notnull.Page].cardSize.ToRDSize()
						});
			throw new Exception($"The word '{word}' is not found in the built book." +
					$" Did you forget to add it using AddWord() or AddWords() before calling Build()?");
		}
	}
}
