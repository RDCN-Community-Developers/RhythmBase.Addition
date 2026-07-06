using RhythmBase.Global.Assets;
using RhythmBase.Global.Components;
using RhythmBase.Global.Components.Vector;
using RhythmBase.RhythmDoctor.Assets;
using RhythmBase.RhythmDoctor.Components;
using RhythmBase.RhythmDoctor.Utils.Projection;
using RhythmBase.RhythmDoctor.Utils.Projection.Isometric;
using SkiaSharp;
using System.Numerics;

namespace RhythmBase.RhythmDoctor.Extensions;

public static partial class Extensions
{
	private static readonly Dictionary<Level, RDAssetManager> assetManagers = [];
	extension(Level level)
	{
		public RDAssetManager AssetManager
		{
			get
			{
				if (!assetManagers.ContainsKey(level))
					assetManagers[level] = new RDAssetManager(level);
				return assetManagers[level];
			}
		}
	}

	public static IAssetFile? GetAsset(this Decoration decoration, RDAssetManager manager)
	{
		string ch = decoration.Character.StringName ?? "";
		if (string.IsNullOrWhiteSpace(ch))
			return null;
		return (IAssetFile?)manager.GetFile<RDSprite>(ch) ?? manager.GetFile<RDImage>(ch);
	}
	public static IAssetFile? GetAsset(this Row row, RDAssetManager manager)
	{
		string? ch = row.Character.StringName;
		if (string.IsNullOrWhiteSpace(ch))
			return null;
		return (IAssetFile?)manager.GetFile<RDSprite>(ch) ?? manager.GetFile<RDImage>(ch);
	}
	public static IAssetFile? GetAsset(this Audio audio, RDAssetManager manager)
	{
		string ch = audio.Filename;
		if (string.IsNullOrWhiteSpace(ch))
			return null;
		return (IAssetFile?)manager.GetFile<RDWaveFile>(ch) ?? manager.GetFile<RDBuiltInAudio>(ch);
	}
	public static PointN3 ToPointN3(this Vector3 vec) => new(vec.X, vec.Y, vec.Z);
	public static Vector3 ToVector3(this PointN3 point) => new(point.X, point.Y, point.Z);

	public static Color ToColor(this SKColor color) => Color.FromRgba(color.Red, color.Green, color.Blue, color.Alpha);
	public static SKColor ToSKColor(this Color color) => new(color.R, color.G, color.B, color.A);
	public static Result3D<Utils.Projection.Isometric.Plane> PointAt(this Result3D<Utils.Projection.Isometric.Room> result, PointN3 pb)
	{
		var p = new PointN3
		{
			X = result.X.CameraValue,
			Y = result.Y.CameraValue,
			Z = result.Z.CameraValue,
		};
		Result3D<Utils.Projection.Isometric.Plane> r = new()
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
	private static float Length(PointN p) => (float)Math.Sqrt(p.X * p.X + p.Y * p.Y);
}