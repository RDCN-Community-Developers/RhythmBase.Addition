namespace RhythmBase.Global.Utils
{
	public class AllocationPool<TState, TResource>(int maxPoolSize)
		where TState : IEquatable<TState>
		where TResource : new()
	{
		public delegate TResource CreateResource(TState state);
		public delegate void StateChanged(TResource resource, TState oldState, TState newState, float time);
		public struct Action(float start, float end, TState target) : IComparable<Action>
		{
			public float Start = start;
			public float End = end;
			public TState Target = target;
			public readonly int CompareTo(Action other) => Start.CompareTo(other.Start);
			public readonly bool Conflicts(Action other) => !(End <= other.Start || Start >= other.End);
			public override readonly string ToString() => $"{Start}->{End} @ {Target}";
		}
		public class Allocation : SortedSet<Action>
		{
			public TState State;
			public TResource Resource;
		}
		public CreateResource OnCreateResource { get; set; } = (state) => new TResource();
		public StateChanged? OnStateChanged { get; set; } = null;
		public List<Allocation> Allocations { get; } = [];
		public int MaxPoolSize { get; } = maxPoolSize;

		public TResource[] Allocate(IEnumerable<Action> actions)
		{
			List<TResource> allocated = [];
			var sortedActions = actions.OrderBy(a => a).ToList();
			foreach (var action in sortedActions)
			{
				int bestIndex = -1;
				TState state = action.Target;

				Allocation[] sortedAllocations = [..Allocations.OrderByDescending(i => i.State.Equals(state))];

				for (int i = 0; i < sortedAllocations.Length; i++)
				{
					var allocation = sortedAllocations[i];
					Action[] existedActions = [.. allocation];
					Action? previous = null;
					Action? next = null;
					for (int j=0;j< existedActions.Length;j++)
					{
						if (existedActions[j].Conflicts(action))
							goto NextAllocation;
						if(existedActions[j].End < action.Start)
							previous = existedActions[j];
						if(next is null && existedActions[j].Start > action.End)
							next = existedActions[j];
					}
					// No conflicts, can use this allocation
					bestIndex = Allocations.IndexOf(sortedAllocations[i]);
					if (previous is Action previousNotNull && !previousNotNull.Target.Equals(state))
						OnStateChanged?.Invoke(allocation.Resource, allocation.State, state, action.Start);
					if(next is Action nextNotNull && !nextNotNull.Target.Equals(state))
						OnStateChanged?.Invoke(allocation.Resource, allocation.State, state, nextNotNull.Start);
					break;
				NextAllocation:;
				}
				if (bestIndex == -1 && Allocations.Count < MaxPoolSize)
				{
					var newAlloc = new Allocation { State = state, Resource = OnCreateResource(state) };
					newAlloc.Add(action);
					Allocations.Add(newAlloc);
					OnStateChanged?.Invoke(newAlloc.Resource, default, state, action.Start);
					bestIndex = Allocations.Count - 1;
				}
				if (bestIndex == -1)
					throw new InvalidOperationException("No available allocation found.");

				Allocations[bestIndex].Add(action);
				Allocations[bestIndex].State = state;

				allocated.Add(Allocations[bestIndex].Resource);
			}
			return [.. allocated];
		}
	}
}