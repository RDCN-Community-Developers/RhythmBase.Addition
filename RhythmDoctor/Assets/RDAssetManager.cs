using RhythmBase.Global.Assets;
using RhythmBase.Global.Components;
using RhythmBase.RhythmDoctor.Components;
namespace RhythmBase.RhythmDoctor.Assets
{
	public class RDAssetManager
	{
		public T? GetFile<T>(string name) where T : IAssetFile<T>
		{
			if (!data.TryGetValue(name, out Lazy<IAssetFile?>? value) || value.Value is null)
				data[name] = new Lazy<IAssetFile?>(() => T.FromFile(typeof(T) == typeof(RDBuiltInAudio) ? name : Path.Combine(baseLevel.ResolvedDirectory, name)));
			return data[name].Value is T v ? v : default;
		}
		public void SetFile<T>(string name, T value) where T : IAssetFile<T> => data[name] = new Lazy<IAssetFile?>(value);
		public T? GetFile<T>(FileReference reference) where T : IAssetFile<T> => GetFile<T>(reference.Path);
		public void SetFile<T>(FileReference reference, T value) where T : IAssetFile<T> => SetFile<T>(reference.Path, value);
		public void SaveAll()
		{
			foreach (var pair in data)
				pair.Value.Value?.Save();
		}
		private readonly Level baseLevel;
		private readonly Dictionary<string, Lazy<IAssetFile?>> data = [];

		internal RDAssetManager(Level level)
		{
			baseLevel = level;
		}
	}
}