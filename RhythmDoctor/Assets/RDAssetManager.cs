using RhythmBase.Global.Assets;
using RhythmBase.RhythmDoctor.Components;
namespace RhythmBase.RhythmDoctor.Assets
{
	public class RDAssetManager
	{
		public T? Get<T>(string name) where T : IAssetFile<T>
		{
			if (!data.TryGetValue(name, out Lazy<IAssetFile?>? value) || value.Value is null)
				data[name] = new Lazy<IAssetFile?>(() => T.Load(typeof(T) == typeof(RDBuiltInAudio) ? name : Path.Combine(baseLevel.Directory, name)));
			return data[name].Value is T v ? v : default;
		}
		public void Set<T>(string name, T value) where T : IAssetFile<T>
		{
			data[name] = new Lazy<IAssetFile?>(value);
		}
		public void SaveAll()
		{
			foreach (var pair in data)
				pair.Value.Value?.Save();
		}
		private readonly RDLevel baseLevel;
		private readonly Dictionary<string, Lazy<IAssetFile?>> data = [];

		internal RDAssetManager(RDLevel level)
		{
			baseLevel = level;
		}
	}
}