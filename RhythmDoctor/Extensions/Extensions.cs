using RhythmBase.Global.Assets;
using RhythmBase.Global.Components;
using RhythmBase.RhythmDoctor.Assets;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Utils.Perspective;
using System.Numerics;

namespace RhythmBase.RhythmDoctor.Extensions
{
	public static class Extensions
	{
		private static readonly Dictionary<RDLevel, RDAssetManager> assetManagers = [];
		public static RDAssetManager GetAssetManager(this RDLevel level)
		{
			if (!assetManagers.ContainsKey(level))
				assetManagers[level] = new RDAssetManager(level);
			return assetManagers[level];
		}
		public static IAssetFile? GetAsset(this Decoration decoration, RDAssetManager manager)
		{
			string ch = decoration.Filename;
			if (string.IsNullOrWhiteSpace(ch))
				return null;
			return (IAssetFile?)manager.Get<RDSprite>(ch) ?? manager.Get<RDImage>(ch);
		}
		public static IAssetFile? GetAsset(this Row row, RDAssetManager manager)
		{
			string? ch = row.Character.CustomCharacter;
			if (string.IsNullOrWhiteSpace(ch))
				return null;
			return (IAssetFile?)manager.Get<RDSprite>(ch) ?? manager.Get<RDImage>(ch);
		}
		public static IAssetFile? GetAsset(this RDAudio audio, RDAssetManager manager)
		{
			string ch = audio.Filename;
			if (string.IsNullOrWhiteSpace(ch))
				return null;
			return (IAssetFile?)manager.Get<RDWaveFile>(ch) ?? manager.Get<RDBuiltInAudio>(ch);
		}
		public static RDPointN3 ToRDPointN3(this Vector3 vec) => new RDPointN3(vec.X, vec.Y, vec.Z);
		public static Vector3 ToVector3(this RDPointN3 point) => new Vector3(point.X, point.Y, point.Z);
		public static Result3D<Utils.Perspective.Plane> PointAt(this Result3D<Room> result, RDPointN3 pb)
		{
			var p = new RDPointN3
			{
				X = result.X.CameraValue,
				Y = result.Y.CameraValue,
				Z = result.Z.CameraValue,
			};
			Result3D<Utils.Perspective.Plane> r = new()
			{
				X = new()
				{
					Position = new(
						pb.Y * -float.Sin(result.X.ElementAngle) +
						pb.Z * float.Cos(result.X.ElementAngle) * float.Sign(p.X),
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
						pb.X * float.Cos(result.Y.ElementAngle) * float.Sign(p.Y),
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
						pb.Y * float.Cos(result.Z.ElementAngle) * float.Sign(p.Z),
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
		private static float Length(RDPointN p) => (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
	}
}