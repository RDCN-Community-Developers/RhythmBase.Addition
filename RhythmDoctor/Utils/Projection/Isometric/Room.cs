using RhythmBase.Global.Components.Vector;
using RhythmBase.RhythmDoctor.Events;

namespace RhythmBase.RhythmDoctor.Utils.Projection.Isometric
{
	public record struct Room
	{
		public PointN RoomDirecion { get; internal set; }
		public float ElementAngle { get; internal set; }
		public PointN Direction { get; internal set; }
		public float CameraValue { get; internal set; }
		public PointN Projection { get; internal set; }
		public readonly MoveRoom GetMoveRoom()
		{
			return new()
			{
				Scale = new SizeN(100, Length(RoomDirecion) * 100),
				Angle = float.Atan2(RoomDirecion.Y, RoomDirecion.X) * 180f / (float)Math.PI,
			};
		}
		private static float Length(PointN p) =>
				(float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
	}
}