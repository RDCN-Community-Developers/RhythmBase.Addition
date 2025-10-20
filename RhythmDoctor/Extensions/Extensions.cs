using RhythmBase.Global.Assets;
using RhythmBase.RhythmDoctor.Assets;
using RhythmBase.RhythmDoctor.Components;
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
	}
}