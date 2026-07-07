using System.Collections.ObjectModel;

namespace RhythmBase.Global.Utils
{
	public class AllocationPool<TReadOnlyState, TWritableState, TResource>(int maxPoolSize)
		where TReadOnlyState : IEquatable<TReadOnlyState>
		where TWritableState : IEquatable<TWritableState>
		where TResource : new()
	{
		public delegate TResource CreateResource(TReadOnlyState state);
		public delegate void StateChanged(TResource resource, TWritableState state, float time);

		public class Fragment
		{
			public float Start { get; }
			public float End { get; }
			public TReadOnlyState ReadOnlyState { get; }
			public TWritableState WritableState { get; }

			public CreateResource? OnCreateResource { get; init; }
			public StateChanged? OnStateChanged { get; init; }
			public Action<Fragment>? OnBuild { get; init; }

			internal bool IsBuilt { get; set; }
			internal Allocation? Owner { get; set; }

			public TResource Resource
			{
				get
				{
					if (!IsBuilt)
						throw new InvalidOperationException(
							"Cannot access Resource before Build().");
					return Owner!.Resource;
				}
			}

			internal Fragment(float start, float end,
				TReadOnlyState readOnlyState, TWritableState writableState)
			{
				Start = start;
				End = end;
				ReadOnlyState = readOnlyState;
				WritableState = writableState;
			}

			public bool Conflicts(Fragment other, bool adjacentConflict)
			{
				if (adjacentConflict)
					return !(End < other.Start || Start > other.End);
				else
					return !(End <= other.Start || Start >= other.End);
			}
		}

		public class Allocation
		{
			public TReadOnlyState ReadOnlyState { get; }
			public TResource Resource { get; }
			private readonly List<Fragment> fragments = [];
			public IReadOnlyList<Fragment> Fragments => fragments;

			internal Allocation(TReadOnlyState readOnlyState, TResource resource)
			{
				ReadOnlyState = readOnlyState;
				Resource = resource;
			}

			internal void Add(Fragment fragment) => fragments.Add(fragment);

			public IEnumerable<Fragment> SortedFragments
				=> fragments.OrderBy(f => f.Start);
		}

		public class BuildResult
		{
			public ReadOnlyCollection<Allocation> Allocations { get; }

			internal BuildResult(ReadOnlyCollection<Allocation> allocations)
				=> Allocations = allocations;
		}

		public CreateResource OnCreateResource { get; set; } = static (state) => new TResource();
		public StateChanged? OnStateChanged { get; set; } = null;
		public int MaxPoolSize { get; } = maxPoolSize;
		public bool AdjacentConflict { get; set; } = false;

		private readonly List<Fragment> fragments = [];
		protected bool built = false;

		public virtual Fragment Allocate(float start, float end,
			TReadOnlyState readOnlyState, TWritableState writableState,
			CreateResource? onCreateResource = null,
			StateChanged? onStateChanged = null,
			Action<Fragment>? onBuild = null)
		{
			if (built)
				throw new InvalidOperationException(
					"Cannot allocate after Build(). Create a new pool instance.");

			var fragment = new Fragment(start, end, readOnlyState, writableState)
			{
				OnCreateResource = onCreateResource,
				OnStateChanged = onStateChanged,
				OnBuild = onBuild,
			};
			fragments.Add(fragment);
			return fragment;
		}

		public virtual BuildResult Build()
		{
			if (built)
				throw new InvalidOperationException("Build() has already been called.");
			built = true;

			var allocations = new List<Allocation>();
			var sortedFragments = fragments.OrderBy(f => f.Start).ToList();

			foreach (var fragment in sortedFragments)
			{
				int bestIndex = -1;

				for (int i = 0; i < allocations.Count; i++)
				{
					var alloc = allocations[i];
					if (!EqualityComparer<TReadOnlyState>.Default
							.Equals(alloc.ReadOnlyState, fragment.ReadOnlyState))
						continue;

					bool hasConflict = false;
					Fragment? previous = null;
					var sorted = alloc.SortedFragments.ToList();

					for (int j = 0; j < sorted.Count; j++)
					{
						if (sorted[j].Conflicts(fragment, AdjacentConflict))
						{
							hasConflict = true;
							break;
						}
						if (sorted[j].End <= fragment.Start)
							previous = sorted[j];
					}

					if (hasConflict)
						continue;

					bestIndex = i;

					var stateChanged = fragment.OnStateChanged ?? this.OnStateChanged;
					if (previous is not null
						&& !EqualityComparer<TWritableState>.Default
								.Equals(previous.WritableState, fragment.WritableState))
					{
						stateChanged?.Invoke(
							alloc.Resource,
							fragment.WritableState,
							fragment.Start);
					}
					break;
				}

				if (bestIndex == -1 && allocations.Count < MaxPoolSize)
				{
					var createResource =
						fragment.OnCreateResource ?? this.OnCreateResource;
					var resource = createResource(fragment.ReadOnlyState);
					var newAlloc = new Allocation(fragment.ReadOnlyState, resource);
					newAlloc.Add(fragment);
					allocations.Add(newAlloc);
					fragment.Owner = newAlloc;

					var stateChanged =
						fragment.OnStateChanged ?? this.OnStateChanged;
					stateChanged?.Invoke(
						resource,
						fragment.WritableState,
						fragment.Start);

					continue;
				}

				if (bestIndex == -1)
					throw new InvalidOperationException(
						$"MaxPoolSize ({MaxPoolSize}) exceeded.");

				allocations[bestIndex].Add(fragment);
				fragment.Owner = allocations[bestIndex];
			}

			foreach (var fragment in fragments)
			{
				fragment.IsBuilt = true;
				fragment.OnBuild?.Invoke(fragment);
			}

			var result = new BuildResult(allocations.AsReadOnly());
			fragments.Clear();
			return result;
		}
	}
}
