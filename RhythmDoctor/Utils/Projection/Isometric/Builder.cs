using RhythmBase.Global.Components.Easing;
using RhythmBase.Global.Components.Vector;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.RhythmDoctor.Extensions;
using System.Collections.ObjectModel;
using System.Numerics;

namespace RhythmBase.RhythmDoctor.Utils.Projection.Isometric
{
	public class Builder
	{
		private readonly Level level;
		private readonly Viewer viewer = new();
		internal Result3D<Room>? lastLook;
		public SingleRoom XIndex { get; set; } = RoomIndex.None;
		public SingleRoom YIndex { get; set; } = RoomIndex.None;
		public SingleRoom ZIndex { get; set; } = RoomIndex.None;
		public ObservableCollection<PerspectivePoint> Points { get; } = [];
		public Builder(Level level)
		{
			this.level = level;
			Points.CollectionChanged += (s, e) =>
			{
				if (e.OldItems != null)
					foreach (PerspectivePoint p in e.OldItems)
						p.parent = null;
				if (e.NewItems != null)
					foreach (PerspectivePoint p in e.NewItems)
						p.parent = this;
			};
		}
		public void Initialize(TickTime beat)
		{
			if (XIndex != RoomIndex.None)
				level.Add(new SetRoomContentMode() { TickTime = beat, Y = XIndex.Value, Mode = ContentMode.AspectFill });
			if (YIndex != RoomIndex.None)
				level.Add(new SetRoomContentMode() { TickTime = beat, Y = YIndex.Value, Mode = ContentMode.AspectFill });
			if (ZIndex != RoomIndex.None)
				level.Add(new SetRoomContentMode() { TickTime = beat, Y = ZIndex.Value, Mode = ContentMode.AspectFill });
		}
		public void LookFrom(PointN3 p, TickTime beat)
		{
			var result = viewer.LookFrom(p);
			lastLook = result;
			if (XIndex != RoomIndex.None)
			{
				MoveRoom mrx = result.X.GetMoveRoom();
				mrx.TickTime = beat;
				mrx.Y = XIndex.Value;
				mrx.Duration = 0;
				level.Add(mrx);
			}
			if (YIndex != RoomIndex.None)
			{
				MoveRoom mry = result.Y.GetMoveRoom();
				mry.TickTime = beat;
				mry.Y = YIndex.Value;
				mry.Duration = 0;
				level.Add(mry);
			}
			if (ZIndex != RoomIndex.None)
			{
				MoveRoom mrz = result.Z.GetMoveRoom();
				mrz.TickTime = beat;
				mrz.Y = ZIndex.Value;
				mrz.Duration = 0;
				level.Add(mrz);
			}
		}
		public void LookFrom(PointN3 p, TickTime beat, float duration, int frameCount, EaseType ease = EaseType.Linear)
		{
			Vector3 oc = viewer.Camera.ToVector3();
			Vector3 cc = p.ToVector3();
			beat = new(level.Calculator, beat);
			for (int i = 0; i < frameCount; i++)
			{
				TickTime b = beat + duration * (i / (float)(frameCount - 1));
				PointN3 curp = Vector3.Lerp(oc, cc, (float)ease.Calculate(i / (float)(frameCount - 1))).ToPointN3();
				LookFrom(curp, b);
				foreach (var point in Points)
				{
					point.MoveTo(point.currentPoint, b, 0, EaseType.Linear);
				}
			}
		}
	}
}
