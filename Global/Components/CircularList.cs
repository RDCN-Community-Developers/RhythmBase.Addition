namespace RhythmBase.Global.Components
{
	public class CircularList<T> : List<T>
	{
		public new T this[int index]
		{
			get
			{
				if (Count == 0)
					throw new InvalidOperationException("The list is empty.");
				int actualIndex = ((index % Count) + Count) % Count; // safe wrap for any integer
				return base[actualIndex];
			}
			set
			{
				if (Count == 0)
					throw new InvalidOperationException("The list is empty.");
				int actualIndex = ((index % Count) + Count) % Count; // safe wrap for any integer
				base[actualIndex] = value;
			}
		}
	}
}