using RhythmBase.Global.Components;
using RhythmBase.RhythmDoctor.Events;

namespace RhythmBase.RhythmDoctor.Utils.Perspective
{
	public record struct Room
	{
		public RDPointN RoomDirecion { get; internal set; }
		public float ElementAngle { get; internal set; }
		public RDPointN Direction { get; internal set; }
		public float CameraValue { get; internal set; }
		public RDPointN Projection { get; internal set; }
		public readonly MoveRoom GetMoveRoom()
		{
			return new()
			{
				Scale = new RDSizeN(100, Length(RoomDirecion) * 100),
				Angle = float.Atan2(RoomDirecion.Y, RoomDirecion.X) * 180f / (float)Math.PI,
			};
		}
		private static float Length(RDPointN p) =>
			(float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
	}
}