namespace RhythmBase.Global.Utils
{
	/// <summary>
	/// 通用区间分配池，将一组区间分配到有限数量的资源池，保证同一池内区间不冲突。
	/// </summary>
	/// <typeparam name="TState">池的状态类型（如当前 Rect），需实现 IEquatable&lt;TState&gt;</typeparam>
	/// <typeparam name="TResource">分配时可用的资源类型</typeparam>
	public class IntervalAllocationPool<TState, TResource>(int maxPoolSize)
		where TState : IEquatable<TState>
		where TResource : new()
	{
		/// <summary>
		/// 表示一个区间分配动作。
		/// </summary>
		public struct Action(float start, float end, TState target) : IComparable<Action>
		{
			public float Start = start;
			public float End = end;
			public TState Target = target;
			public readonly int CompareTo(Action other) => Start.CompareTo(other.Start);
			public readonly bool Conflicts(Action other) => !(End <= other.Start || Start >= other.End);
			public override readonly string ToString() => $"{Start}->{End} @ {Target}";
		}
		public class Pool : SortedSet<Action>
		{
			public TState State;
			public TResource Resource;
		}

		public List<Pool> Pools { get; } = [];
		public int MaxPoolSize { get; } = maxPoolSize;

		/// <summary>
		/// 分配区间到池
		/// </summary>
		public void Allocate(IEnumerable<Action> actions)
		{
			var sortedActions = actions.OrderBy(a => a).ToList();
			foreach (var action in sortedActions)
			{
				int bestIndex = -1;
				TState state = action.Target;

				// 只找资源一致且无冲突的池，状态可以切换
				for (int i = 0; i < Pools.Count; i++)
				{
					var pool = Pools[i];
					if (pool.All(a => !a.Conflicts(action)))
					{
						bestIndex = i;
						break;
					}
				}
				// 新建池
				if (bestIndex == -1 && Pools.Count < MaxPoolSize)
				{
					var newPool = new Pool { State = state, Resource = new() };
					newPool.Add(action);
					Pools.Add(newPool);
					continue;
				}
				if (bestIndex == -1)
					throw new InvalidOperationException("No available pool found.");

				Pools[bestIndex].Add(action);
				Pools[bestIndex].State = state; // 只更新状态，不更新资源
			}
		}
	}
}