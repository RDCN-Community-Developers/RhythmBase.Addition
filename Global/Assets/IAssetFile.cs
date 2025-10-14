namespace RhythmBase.Global.Assets
{
	public interface IAssetFile
	{
		string DisplayName { get; }
		string FilePath { get; }
		abstract void Save(string filepath);
		void Save() => Save(FilePath);
	}
	/// <summary>
	/// Store data.
	/// </summary>
	public interface IAssetFile<T> : IAssetFile where T : IAssetFile<T>
	{
		static abstract T? Load(string fullpath);
	}
}