using RhythmBase.Global.Components;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;

namespace RhythmBase.RhythmDoctor.Utils.Perspective
{
	public record struct Plane
	{
		public RDPointN Position;
		public float Angle;
		public RDSizeN Scale;
		public readonly Move GetMove()
		{
			return new Move
			{
				Angle = Angle * 180f / float.Pi,
				Pivot = new(0,0),
				Position = new(50 + Position.X / 3.52f, 50 + Position.Y / 1.98f),
				Scale = Scale,
			};
		}
	}
}