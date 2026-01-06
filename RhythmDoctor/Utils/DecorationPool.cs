using RhythmBase.Global.Components;
using RhythmBase.Global.Utils;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.Global.Extensions;

namespace RhythmBase.RhythmDoctor.Utils;

public class DecorationPool<TKey> : IDisposable
	where TKey : IEquatable<TKey>
{
	private readonly RDLevel level;
	private readonly string filename;
	private readonly AllocationPool<TKey, Decoration> allocationPool;
	private readonly List<AllocationPool<TKey, Decoration>.Action> actions = [];
	public DecorationPool(RDLevel level, string filename,
		Action<Decoration, TKey, TKey, float>? onStateChanged = null,
		int maxPoolSize = 1000)
	{
		this.level = level;
		this.filename = filename;
		allocationPool = new(maxPoolSize)
		{
			OnCreateResource = (key) =>
			{
				Decoration deco = new Decoration()
				{
					Filename = filename,
				};
				return deco;
			},
			OnStateChanged = (deco, oldKey, newKey, time) =>
			{
				onStateChanged?.Invoke(deco, oldKey, newKey, time); ;
			},
		};
	}

	private void Flush()
	{
		allocationPool.Allocate(actions);

		foreach (var pool in allocationPool.Allocations)
		{
			Decoration deco = new() { Filename = filename };
			level.Decorations.Add(deco);

			var sortedRanges = pool.OrderBy(a => a.Start).ToArray();
			for (int i = 0; i < sortedRanges.Length; i++)
			{
				var v = sortedRanges[i];
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
			}
		}
	}
	public void Dispose()
	{
		Flush();
		GC.SuppressFinalize(this);
	}
	public Decoration Allocate(float startBeat, float endBeat, TKey target, int? y = null, RDSingleRoom? room = null)
	{
		AllocationPool<TKey, Decoration>.Action action = new(startBeat, endBeat, target);
		actions.Add(action);
		var deco = allocationPool.Allocate([action])[0];
		level.Decorations.Add(deco);
		deco.Add(new SetVisible() { Beat = new(level.Calculator, startBeat), Visible = true });
		if (endBeat < float.MaxValue)
			deco.Add(new SetVisible() { Beat = new(level.Calculator, endBeat), Visible = false });
		return deco;

	}
}
