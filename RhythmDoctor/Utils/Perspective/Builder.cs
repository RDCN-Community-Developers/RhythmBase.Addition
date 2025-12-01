using RhythmBase.Global.Components.Easing;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Events;
using RhythmBase.RhythmDoctor.Extensions;
using System.Collections.ObjectModel;
using System.Numerics;

namespace RhythmBase.RhythmDoctor.Utils.Perspective
{
	public class Builder
	{
		private readonly RDLevel level;
		private readonly Viewer viewer = new();
		internal Result3D<Room>? lastLook;
		public RDSingleRoom XIndex { get; set; } = RDRoomIndex.None;
		public RDSingleRoom YIndex { get; set; } = RDRoomIndex.None;
		public RDSingleRoom ZIndex { get; set; } = RDRoomIndex.None;
		public ObservableCollection<PerspectivePoint> Points { get; } = [];
		public Builder(RDLevel level)
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
		public void Initialize(RDBeat beat)
		{
			if (XIndex != RDRoomIndex.None)
				level.Add(new SetRoomContentMode() { Beat = beat, Y = 0, Mode = ContentModes.AspectFill });
			if (XIndex != RDRoomIndex.None)
				level.Add(new SetRoomContentMode() { Beat = beat, Y = 1, Mode = ContentModes.AspectFill });
			if (XIndex != RDRoomIndex.None)
				level.Add(new SetRoomContentMode() { Beat = beat, Y = 2, Mode = ContentModes.AspectFill });
		}
		public void LookFrom(RDPointN3 p, RDBeat beat)
		{
			var result = viewer.LookFrom(p);
			lastLook = result;
			if (XIndex != RDRoomIndex.None)
			{
				MoveRoom mrx = result.X.GetMoveRoom();
				mrx.Beat = beat;
				mrx.Y = XIndex.Value;
				mrx.Duration = 0;
				level.Add(mrx);
			}
			if (YIndex != RDRoomIndex.None)
			{
				MoveRoom mry = result.Y.GetMoveRoom();
				mry.Beat = beat;
				mry.Y = YIndex.Value;
				mry.Duration = 0;
				level.Add(mry);
			}
			if (ZIndex != RDRoomIndex.None)
			{
				MoveRoom mrz = result.Z.GetMoveRoom();
				mrz.Beat = beat;
				mrz.Y = ZIndex.Value;
				mrz.Duration = 0;
				level.Add(mrz);
			}
		}
		public void LookFrom(RDPointN3 p, RDBeat beat, float duration, int frameCount, EaseType ease = EaseType.Linear)
		{
			Vector3 oc = viewer.Camera.ToVector3();
			Vector3 cc = p.ToVector3();
			beat = new(level.Calculator, beat);
			for (int i = 0; i < frameCount; i++)
			{
				RDBeat b = beat + duration * (i / (float)(frameCount - 1));
				RDPointN3 curp = Vector3.Lerp(oc, cc, (float)ease.Calculate(i / (float)(frameCount - 1))).ToRDPointN3();
				LookFrom(curp, b);
				foreach (var point in Points)
				{
					point.MoveTo(point.currentPoint, b, 0, EaseType.Linear);
				}
			}
		}
	}
}
