using RhythmBase.Global.Components.Vector;
using RhythmBase.RhythmDoctor.Events;
using static RhythmBase.RhythmDoctor.Extensions.Extensions;

namespace RhythmBase.RhythmDoctor.Utils.Projection.Isometric
{
	public record struct Plane
	{
		public PointN Position;
		public float Angle;
		public SizeN Scale;
		public readonly Move GetMove()
		{
			PointN pixelPos = new(
				PixelateWidth / 2f + Position.X,
				PixelateHeight / 2f + Position.Y
			);
			return new Move
			{
				Angle = Angle * 180f / float.Pi,
				Pivot = new(0, 0),
				PPosition = pixelPos,
				Scale = Scale,
			};
		}
	}
}