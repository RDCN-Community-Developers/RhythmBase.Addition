using RhythmBase.Global.Components;
using RhythmBase.Global.Components.Easing;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Extensions;
using System.Numerics;

namespace RhythmBase.RhythmDoctor.Utils.Perspective
{
	public class Viewer
	{
		public RDPointN3 Camera { get; private set; }
		//public float Zoom { get; private set; } = 1.0f;
		public Viewer()
		{
		}
		public Result3D<Room> LookFrom(RDPointN3 p)
		{
			var v = p.ToVector3();
			Camera = p;
			// 基平面投影基准向量
			var ux = UnitOf(new(1, 0, 0), v);
			var uy = UnitOf(new(0, 1, 0), v);
			var uz = UnitOf(new(0, 0, 1), v);
			// 投影向量
			var vx = Project(ux, v);
			var vy = Project(uy, v);
			var vz = Project(uz, v);
			// 空间夹角
			var rx = CosOf(ux, new Vector3(0, 1, 0));
			var ry = CosOf(uy, new Vector3(0, 0, 1));
			var rz = CosOf(uz, new Vector3(1, 0, 0));
			// 法向量投影
			var tx = Project(new(1, 0, 0), v);
			var ty = Project(new(0, 1, 0), v);
			var tz = Project(new(0, 0, 1), v);

			var r = AngleOf(p.ToVector3());

			var bx = float.Sign(p.X);
			var by = float.Sign(p.Y);
			var bz = float.Sign(p.Z);
			Result3D<Room> result = new()
			{
				X = new()
				{
					Projection = new RDPointN(tx.X, tx.Y),
					RoomDirecion = new RDPointN(vx.X, -vx.Y),
					ElementAngle = rx * bx * bz,
					Direction = new RDPointN(tx.X, tx.Y),
					CameraValue = p.X,
				},
				Y = new()
				{
					Projection = new RDPointN(ty.X, ty.Y),
					RoomDirecion = new RDPointN(vy.X, vy.Y),
					ElementAngle = ry * bx * by,
					Direction = new RDPointN(ty.X, ty.Y),
					CameraValue = p.Y,
				},
				Z = new()
				{
					Projection = new RDPointN(tz.X, tz.Y),
					RoomDirecion = new RDPointN(vz.X, -vz.Y),
					ElementAngle = rz * bz * by,
					Direction = new RDPointN(tz.X, tz.Y),
					CameraValue = p.Z,
				},
			};
			return result;
		}
		public Result3D<Room>[] LookFrom(RDPointN3 p, int frameCount, EaseType ease = EaseType.Linear)
		{
			Vector3 oc = Camera.ToVector3();
			Vector3 cc = p.ToVector3();
			Result3D<Room>[] results = new Result3D<Room>[frameCount];
			for (int i = 0; i < frameCount; i++)
			{
				float t = (float)ease.Calculate(i / (float)(frameCount - 1));
				Vector3 c = Vector3.Lerp(oc, cc, t);
				results[i] = LookFrom(new RDPointN3(c.X, c.Y, c.Z));
			}
			return results;
		}
		private static Vector2 Project(Vector3 value, Vector3 dir)
		{
			const float EPS = 1e-6f;
			const float EPS_SQ = EPS * EPS;
			float dirLenSq = Vector3.Dot(dir, dir);
			if (dirLenSq <= EPS_SQ)
				return Vector2.Zero;
			Vector3 nDir = Vector3.Normalize(dir);
			float comp = Vector3.Dot(value, nDir);
			Vector3 vproj = value - nDir * comp;
			if (vproj.LengthSquared() <= EPS_SQ)
				return Vector2.Zero;
			Vector3 refAxis = new Vector3(0f, 1f, 0f);
			if (Vector3.Cross(refAxis, nDir).LengthSquared() <= EPS_SQ)
				refAxis = new Vector3(1f, 0f, 0f);
			Vector3 u = refAxis - nDir * Vector3.Dot(refAxis, nDir);
			if (u.LengthSquared() <= EPS_SQ)
				return Vector2.Zero;
			u = Vector3.Normalize(u);
			Vector3 v = Vector3.Cross(nDir, u);
			if (v.LengthSquared() <= EPS_SQ)
				return Vector2.Zero;
			v = Vector3.Normalize(v);
			return new Vector2(Vector3.Dot(vproj, u), Vector3.Dot(vproj, v));
		}
		private static Vector3 UnitOf(Vector3 unit, Vector3 dir)
		{
			const float EPS = 1e-6f;

			float unitLenSq = Vector3.Dot(unit, unit);
			if (unitLenSq <= EPS)
				return Vector3.Zero;

			float dotUD = Vector3.Dot(unit, dir);
			Vector3 res = dir * unitLenSq - unit * dotUD;

			if (res.LengthSquared() <= EPS * EPS)
				return Vector3.Zero;

			return Vector3.Normalize(res);
		}
		private static float CosOf(Vector3 v1, Vector3 v2)
		{
			float len1 = v1.Length();
			float len2 = v2.Length();
			if (len1 <= 1e-6f || len2 <= 1e-6f)
				return 0f;
			return float.Acos(Vector3.Dot(v1, v2) / (len1 * len2));
		}
		private static Vector2 AngleOf(Vector3 v)
		{
			const float EPS = 1e-6f;
			if (v.LengthSquared() <= EPS * EPS)
				return Vector2.Zero;
			float proj = MathF.Sqrt(v.X * v.X + v.Z * v.Z);
			float yaw = 0f;
			if (proj > EPS)
				yaw = MathF.Atan2(v.X, v.Z);
			float pitch = MathF.Atan2(v.Y, proj);
			return new Vector2(yaw, pitch);
		}
	}
}
