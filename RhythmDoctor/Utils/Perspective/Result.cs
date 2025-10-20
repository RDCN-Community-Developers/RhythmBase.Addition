namespace RhythmBase.RhythmDoctor.Utils.Perspective
{
	public struct Result<T> where T : IResult
	{
		public T X;
		public T Y;
		public T Z;
	}
}
