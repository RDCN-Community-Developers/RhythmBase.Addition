using RhythmBase.Global.Components;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Extensions;
using System.Numerics;

namespace RhythmBase.RhythmDoctor.Utils.Perspective
{
	public class Viewer
	{
		private Vector3 View { get; set; }
		public float Zoom { get; private set; } = 1.0f;
		public RDPointN Center { get; private set; } = new RDPointN(0, 0);
		public Viewer()
		{
		}
		public Result<ObjectResult> PointAt(RDPointN3 pb)
		{
			var p = View;
			var result = LookFrom(p.ToRDPointN3());
			Result<ObjectResult> r = new()
			{
				X = new()
				{
					Position = new(

						pb.Y * -float.Sin(result.X.ElementAngle) +
						pb.Z * float.Cos(result.X.ElementAngle) * float.Sign(p.X) +
						0
						,

						pb.Y * float.Cos(result.X.ElementAngle) +
						pb.Z * float.Sin(result.X.ElementAngle) * float.Sign(p.X) +
						pb.X * Length(result.X.Direction) / (Length(result.X.RoomDirecion) + 0.00001f) * float.Sign(-p.X)
						),
					Angle = result.X.ElementAngle,
					Scale = new(float.Sign(p.X), 1),
				},
				Y = new()
				{
					Position = new(

						pb.Z * -float.Sin(result.Y.ElementAngle) +
						pb.X * float.Cos(result.Y.ElementAngle) * float.Sign(p.Y) +
						0
						,

						pb.Z * float.Cos(result.Y.ElementAngle) +
						pb.X * float.Sin(result.Y.ElementAngle) * float.Sign(p.Y) +
						pb.Y * Length(result.Y.Direction) / (Length(result.Y.RoomDirecion) + 0.00001f) * float.Sign(-p.Y)
						),
					Angle = result.Y.ElementAngle,
					Scale = new(float.Sign(p.Y), 1),
				},
				Z = new()
				{
					Position = new(

						pb.X * -float.Sin(result.Z.ElementAngle) +
						pb.Y * float.Cos(result.Z.ElementAngle) * float.Sign(p.Z) +
						0
						,

						pb.X * float.Cos(result.Z.ElementAngle) +
						pb.Y * float.Sin(result.Z.ElementAngle) * float.Sign(p.Z) +
						pb.Z * Length(result.Z.Direction) / (Length(result.Z.RoomDirecion) + 0.00001f) * float.Sign(-p.Z)
						),
					Angle = result.Z.ElementAngle,
					Scale = new(float.Sign(p.Z), 1),
				},
			};
			return r;
		}
		private static float Length(RDPointN p) =>
			(float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
		public Result<RoomResult> LookFrom(RDPointN3 p)
		{
			var v = p.ToVector3();
			View = v;
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
			Result<RoomResult> result = new()
			{
				X = new()
				{
					RoomDirecion = new RDPointN(vx.X, -vx.Y),
					ElementAngle = rx * bx * bz,
					Direction = new RDPointN(tx.X, tx.Y),
				},
				Y = new()
				{
					RoomDirecion = new RDPointN(vy.X, vy.Y),
					ElementAngle = ry * bx * by,
					Direction = new RDPointN(ty.X, ty.Y),
				},
				Z = new()
				{
					RoomDirecion = new RDPointN(vz.X, -vz.Y),
					ElementAngle = rz * bz * by,
					Direction = new RDPointN(tz.X, tz.Y),
				},
			};
			return result;
		}
		private static Vector2 Project(Vector3 value, Vector3 dir)
		{
			/* 伪代码（详细计划）：
			- 如果 dir 近似为零向量，返回 Vector2.Zero（无法定义平面方向）
			- 归一化 dir 为 nDir（这样可以简化投影计算）
			- 计算 value 在法向量方向的分量并从 value 中减去，得到 vproj（即 value 在平面上的向量）
			- 如果 vproj 非常接近 0，返回 Vector2.Zero（投影近似为点）
			- 选择参考轴 refAxis（优先 Y 轴），如果与 nDir 平行则改用 X 轴
			- 通过 Gram-Schmidt 正交化 refAxis 得到 u（在平面内并与 nDir 正交），归一化
			- 计算 v = normalize(cross(nDir, u))（在平面内且与 u 正交）
			- 将 vproj 在 (u, v) 基底上的坐标作为返回值
			- 在每一步都用一致的 EPS（对长度平方比较使用 EPS*EPS）来判断退化情况
			*/
			const float EPS = 1e-6f;
			const float EPS_SQ = EPS * EPS;

			// dir 不能为零向量
			float dirLenSq = Vector3.Dot(dir, dir);
			if (dirLenSq <= EPS_SQ)
				return Vector2.Zero;

			// 归一化法向量，便于后续计算
			Vector3 nDir = Vector3.Normalize(dir);

			// 在平面上的投影：value 去除沿法向量的分量
			float comp = Vector3.Dot(value, nDir);
			Vector3 vproj = value - nDir * comp;
			if (vproj.LengthSquared() <= EPS_SQ)
				return Vector2.Zero;

			// 选择参考轴（优先 Y 轴，避免与法向量共线）
			Vector3 refAxis = new Vector3(0f, 1f, 0f);
			if (Vector3.Cross(refAxis, nDir).LengthSquared() <= EPS_SQ)
				refAxis = new Vector3(1f, 0f, 0f);

			// Gram-Schmidt：把 refAxis 正交到平面上，得到 u
			Vector3 u = refAxis - nDir * Vector3.Dot(refAxis, nDir);
			if (u.LengthSquared() <= EPS_SQ)
				return Vector2.Zero;
			u = Vector3.Normalize(u);

			// 在平面内与 u 正交的方向 v
			Vector3 v = Vector3.Cross(nDir, u);
			if (v.LengthSquared() <= EPS_SQ)
				return Vector2.Zero;
			v = Vector3.Normalize(v);

			// 返回在 (u, v) 基底上的坐标
			return new Vector2(Vector3.Dot(vproj, u), Vector3.Dot(vproj, v));
		}

		// 伪代码（详细计划）：
		// 1. 计算 unit 的长度平方 unitLenSq；如果接近 0，返回 Vector3.Zero（unit 不能为零向量）
		// 2. 计算点积 dotUD = dot(unit, dir)
		// 3. 利用向量三重积恒等式计算交线方向向量：res = dir * unitLenSq - unit * dotUD
		//    说明：res 等价于 (unit × dir) × unit，位于由 unit 与 dir 张成的平面与垂直于 unit 的平面的交线上
		// 4. 如果 res 近似为零（表示 dir 与 unit 共线或退化），返回 Vector3.Zero
		// 5. 返回归一化后的 res 作为交线的单位方向向量
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
		// 偏航角（yaw），俯仰角（pitch）
		/* 详细计划（伪代码）：
        - 如果向量 v 的长度接近 0，返回 Vector2.Zero（无方向）
        - 计算在 XZ 平面上的投影长度 proj = sqrt(v.X^2 + v.Z^2)
        - 偏航角 yaw：绕 Y 轴的角度，使用 atan2(v.X, v.Z)
        - 俯仰角 pitch：绕 X 轴（水平轴）的角度，使用 atan2(v.Y, proj)
        - 说明：返回的角度均为弧度。若 proj 非常接近 0，则将 yaw 设为 0（朝向上/下时无法定义偏航）
        */
		private static Vector2 AngleOf(Vector3 v)
		{
			const float EPS = 1e-6f;

			// 向量太小则无法确定角度
			if (v.LengthSquared() <= EPS * EPS)
				return Vector2.Zero;

			// 在 XZ 平面上的投影长度
			float proj = MathF.Sqrt(v.X * v.X + v.Z * v.Z);

			// 偏航：绕 Y 轴（右手系），以 Z 轴为参考，右偏为正
			float yaw = 0f;
			if (proj > EPS)
				yaw = MathF.Atan2(v.X, v.Z);

			// 俯仰：绕 X 轴，向上为正
			float pitch = MathF.Atan2(v.Y, proj);

			return new Vector2(yaw, pitch);
		}
	}
}
