using RhythmBase.Global.Utils;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;

namespace RhythmBase.RhythmDoctor.Utils;

/// <summary>
/// 装饰池。TReadOnlyState 为文件引用等不可变状态，
/// TWritableState 为动画 Key 等可写状态。
/// </summary>
public class DecorationPool<TReadOnlyState, TWritableState>
	: AllocationPool<TReadOnlyState, TWritableState, Decoration>, IDisposable
	where TReadOnlyState : IEquatable<TReadOnlyState>
	where TWritableState : IEquatable<TWritableState>
{
	protected Level level;
	protected readonly string filename;

	public DecorationPool(
		Level level,
		string filename,
		int maxPoolSize = 1000)
		: base(maxPoolSize)
	{
		this.level = level;
		this.filename = filename;
		OnCreateResource = static (state) => new Decoration();
	}

	public override BuildResult Build()
	{
		var result = base.Build();

		foreach (var allocation in result.Allocations)
		{
			Decoration deco = allocation.Resource;
			deco.Character = filename;
			level.Decorations.Add(deco);

			var sortedFragments = allocation.SortedFragments.ToList();
			for (int i = 0; i < sortedFragments.Count; i++)
			{
				var v = sortedFragments[i];
				if (i == 0)
					deco.Add(new SetVisible
					{
						TickTime = new(level.Calculator, v.Start),
						Visible = true
					});
				else if (sortedFragments[i - 1].End < v.Start)
				{
					deco.Add(new SetVisible
					{
						TickTime = new(level.Calculator, sortedFragments[i - 1].End),
						Visible = false
					});
					deco.Add(new SetVisible
					{
						TickTime = new(level.Calculator, v.Start),
						Visible = true
					});
				}
			}
			if (sortedFragments.Count > 0
				&& sortedFragments[^1].End < float.MaxValue)
			{
				deco.Add(new SetVisible
				{
					TickTime = new(level.Calculator, sortedFragments[^1].End),
					Visible = false
				});
			}
		}

		return result;
	}

	public void Dispose()
	{
		Build();
		GC.SuppressFinalize(this);
	}
}
