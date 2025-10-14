using RhythmBase.Global.Assets;

namespace RhythmBase.RhythmDoctor.Assets
{
	public class RDBuiltInAudio(string name) : IAssetFile<RDBuiltInAudio>
	{
		public string DisplayName { get; } = name;
		public string FilePath => "";
		public static RDBuiltInAudio? Load(string name) => new(name);
		void IAssetFile.Save(string filepath) { }
		public override string ToString() => DisplayName;
	}
}