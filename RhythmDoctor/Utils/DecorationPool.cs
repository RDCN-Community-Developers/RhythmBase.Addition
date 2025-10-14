using RhythmBase.Global.Components;
using RhythmBase.Global.Utils;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;

public class DecorationPool<TKey> : IDisposable
	where TKey : IEquatable<TKey>
{
	private readonly RDLevel level;
	private readonly string filename;
	private readonly IntervalAllocationPool<TKey, Decoration> allocationPool;
	private readonly List<IntervalAllocationPool<TKey, Decoration>.Action> actions = [];

	public DecorationPool(RDLevel level, string filename, int maxPoolSize = 1000)
	{
		this.level = level;
		this.filename = filename;
		allocationPool = new(maxPoolSize);
	}

	private void Flush()
	{
		allocationPool.Allocate(actions);

		foreach (var pool in allocationPool.Pools)
		{
			Decoration deco = new() { Filename = filename };
			level.Decorations.Add(deco);

			var sortedRanges = pool.OrderBy(a => a.Start).ToArray();
			for (int i = 0; i < sortedRanges.Length; i++)
			{
				var v = sortedRanges[i];
				// 这里可根据原 Range 结构补充 y/room 等信息
				if (i == 0)
					deco.Add(new SetVisible() { Beat = new(level.Calculator, v.Start), Visible = true });
				else
				{
					if (sortedRanges[i - 1].End < v.Start)
					{
						deco.Add(new SetVisible() { Beat = new(level.Calculator, sortedRanges[i - 1].End), Visible = false });
						deco.Add(new SetVisible() { Beat = new(level.Calculator, v.Start), Visible = true });
					}
					if (i == sortedRanges.Length - 1 && v.End < float.MaxValue)
						deco.Add(new SetVisible() { Beat = new(level.Calculator, v.End), Visible = false });
				}
				// 如需支持 y/room，可在 Action 结构中扩展
			}
		}
	}

	public void Dispose() => Flush();
	public Decoration Allocate(float startBeat, float endBeat, TKey target, int? y = null, RDSingleRoom? room = null)
	{
		actions.Add(new(startBeat, endBeat, target));
		allocationPool.Allocate(actions);
		actions.Clear();

		// 查找刚分配的资源
		foreach (var pool in allocationPool.Pools)
		{
			foreach (var action in pool)
			{
				if (action.Start == startBeat && action.End == endBeat && action.Target.Equals(target))
				{
					Decoration deco = new() { Filename = filename };
					level.Decorations.Add(deco);
					deco.Add(new SetVisible() { Beat = new(level.Calculator, startBeat), Visible = true });
					if (endBeat < float.MaxValue)
						deco.Add(new SetVisible() { Beat = new(level.Calculator, endBeat), Visible = false });
					return deco;
				}
			}
		}
		return null!;
	}
}
