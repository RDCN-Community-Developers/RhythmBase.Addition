namespace RhythmBase.RhythmDoctor.Assets.FlexibleSprite
{
	public class SpriteSheetBook
	{
		public int PageCount = 0;
		public List<PageInfo> PageInfos { get; } = [];
		public List<PageIndex> CardIndice { get; } = [];
		public PageIndex this[int index] => index < 0 || index >= CardIndice.Count
					? throw new ArgumentOutOfRangeException(nameof(index), $"Index {index} is out of range for the sprite sheet book with {CardIndice.Count} pages.")
					: CardIndice.Single(i => i.ImageIndex == index);
	}
}
