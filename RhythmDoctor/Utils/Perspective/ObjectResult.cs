using RhythmBase.Global.Components;

namespace RhythmBase.RhythmDoctor.Utils.Perspective
{
	public struct ObjectResult : IResult
	{
		public RDPointN Position;
		public float Angle;
		public RDSizeN Scale;
		IInternal IResult.Internal { readonly get; set; }
	}
}
