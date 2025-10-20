using RhythmBase.Global.Components;

namespace RhythmBase.RhythmDoctor.Utils.Perspective
{
	public struct RoomResult : IResult
	{
		public RDPointN RoomDirecion;
		public float ElementAngle;
		public RDPointN Direction;
		IInternal IResult.Internal { readonly get; set; }
	}
}
