using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Extensions;
using RhythmBase.Global.Utils;
using RhythmBase.RhythmDoctor.Assets;
using RhythmBase.RhythmDoctor.Assets.FlexibleSprite;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using SkiaSharp;
using static RhythmBase.RhythmDoctor.Extensions.Extensions;

namespace RhythmBase.RhythmDoctor.Utils
{
	public record struct TextInfo(string Character, PointN Position, RectN Bound, float AdvanceX, SizeNI Size)
	{
		public readonly PointN DecorationPivot => Position.ToPercentagePoint(Size);
	}

	/// <summary>
	/// 文字字体管理器。继承 DecorationPool 统一 API。
	/// 内部按 Sprite Page 拆分为多个子池。
	/// </summary>
	public class TextFontManager : DecorationPool<string, string>
	{
		private readonly Dictionary<string, TextInfo> infos = [];
		private readonly SKFont font;
		private readonly SpriteSheetCanvas spriteSheetCanvas;
		private SpriteSheetBook? builtBook = null;
		private DecorationPool<string, string>[] pagePools = [];

		public TextFontManager AddWord(string word)
		{
			if (infos.TryGetValue(word, out _))
				return this;
			var rect = spriteSheetCanvas.DrawText(word, font, out var pos, out var width);
			TextInfo newInfo = new(word, pos.ToRDPoint(), rect.ToRect(), width, default);
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
			: base(null!, filename)
		{
			this.font = font;
			spriteSheetCanvas = new SpriteSheetCanvas(settings);
		}

		/// <summary>
		/// 构建 Sprite Sheet 并创建各 Page 的子池。
		/// </summary>
		public RDSprite[] PrepareSprites(Level level, int maxPoolSize = 1000)
		{
			this.level = level;
			builtBook = spriteSheetCanvas.Build(out RDSprite[] result);
			foreach (var s in result)
			{
				foreach (var clip in s.Clips)
					clip.Loop = LoopOption.onTickTime;
				s.AddBlankExpressionForDecoration();
			}
			foreach (var (word, info) in infos)
			{
				PageIndex? page = builtBook.CardIndice?
					.FirstOrDefault(i => i.Name == word);
				if (page is PageIndex notnull)
					infos[word] = info with
					{
						Size = builtBook.PageInfos[notnull.Page].cardSize.ToRDSize()
					};
			}
			pagePools = new DecorationPool<string, string>[builtBook.PageCount];
			for (int i = 0; i < pagePools.Length; i++)
			{
				pagePools[i] = new DecorationPool<string, string>(
					level, $"{filename}-{i}",
					maxPoolSize);
				pagePools[i].OnStateChanged = (deco, key, beat) =>
				{
					deco.Add(new PlayAnimation()
					{
						TickTime = new(beat),
						Expression = key.WithUppercasePrefix(),
					});
				};
			}
			return result;
		}

		/// <summary>
		/// 记录分配意图，路由到正确的 Page 子池。
		/// readOnlyState 和 writableState 均传入 word 即可。
		/// </summary>
		public override Fragment Allocate(float start, float end,
			string readOnlyState, string writableState,
			CreateResource? onCreateResource = null,
			StateChanged? onStateChanged = null,
			Action<Fragment>? onBuild = null)
		{
			if (builtBook is null)
				throw new InvalidOperationException(
					"You must call PrepareSprites() before Allocate().");
			PageIndex? page = builtBook.CardIndice?
				.First(i => i.Name == writableState);
			if (page is PageIndex notnull)
				return pagePools[notnull.Page].Allocate(
					start, end, readOnlyState, writableState,
					onCreateResource, onStateChanged, onBuild);
			throw new Exception(
				$"The word '{writableState}' is not found in the built book." +
				$" Did you forget to add it using AddWord() or AddWords() before calling PrepareSprites()?");
		}

		/// <summary>
		/// 构建所有 Page 子池，返回聚合结果。
		/// </summary>
		public override BuildResult Build()
		{
			if (built)
				throw new InvalidOperationException("Build() has already been called.");
			built = true;

			var allAllocations = new List<Allocation>();
			foreach (var pool in pagePools)
			{
				var result = pool.Build();
				foreach (var alloc in result.Allocations)
					allAllocations.Add(alloc);
			}
			return new BuildResult(allAllocations.AsReadOnly());
		}

		/// <summary>
		/// 便捷 Allocate：仅传入 word，同时作为 readOnlyState 和 writableState。
		/// </summary>
		public Fragment Allocate(float start, float end, string word)
			=> Allocate(start, end, word, word);

		/// <summary>
		/// 获取已注册文字的排版信息。
		/// </summary>
		public TextInfo GetTextInfo(string word) => infos[word];
	}
}
