using RhythmBase.Global.Components.Easing;
using RhythmBase.Global.Components.Vector;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.RhythmDoctor.Extensions;

namespace RhythmBase.RhythmDoctor.Utils.Projection.Isometric
{
	public class PerspectivePoint
	{
		internal Builder? parent;
		internal PointN3 currentPoint;
		public List<Decoration> XDecorations { get; set; } = [];
		public List<Decoration> YDecorations { get; set; } = [];
		public List<Decoration> ZDecorations { get; set; } = [];
		public void MoveTo(PointN3 p, TickTime beat, float duration, EaseType ease = EaseType.Linear)
		{
			if (parent?.lastLook is not Result3D<Room> look)
				return;
			currentPoint = p;
			var po = look.PointAt(p);
			foreach (var dec in XDecorations)
			{
				Move m = po.X.GetMove();
				m.TickTime = beat;
				m.Duration = duration;
				m.Ease = ease;
				dec.Add(m);
			}
			foreach (var dec in YDecorations)
			{
				Move m = po.Y.GetMove();
				m.TickTime = beat;
				m.Duration = duration;
				m.Ease = ease;
				dec.Add(m);
			}
			foreach (var dec in ZDecorations)
			{
				Move m = po.Z.GetMove();
				m.TickTime = beat;
				m.Duration = duration;
				m.Ease = ease;
				dec.Add(m);
			}
		}
	}
}
