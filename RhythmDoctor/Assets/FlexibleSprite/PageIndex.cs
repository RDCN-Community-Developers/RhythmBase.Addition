using System.Diagnostics;

namespace RhythmBase.RhythmDoctor.Assets.FlexibleSprite
{
	[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
	public record struct PageIndex(int Page, int ImageIndex, int FrameIndex)
	{
		public string Name { get; set; } = string.Empty;
		public override readonly string ToString() => $"Image {ImageIndex} in p{Page}:{FrameIndex}";
		private readonly string GetDebuggerDisplay() => ToString();
	}
}
