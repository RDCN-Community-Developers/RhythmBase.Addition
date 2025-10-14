using NAudio.Vorbis;
using NAudio.Wave;
using RhythmBase.Global.Assets;
using System.Diagnostics.CodeAnalysis;
namespace RhythmBase.RhythmDoctor.Assets
{
	public class RDWaveFile : IAssetFile<RDWaveFile>
	{
		private string filePath;
		public string DisplayName => Path.GetFileName(FilePath);
		public required string FilePath
		{
			get => filePath;
			[MemberNotNull(nameof(filePath))]
			[MemberNotNull(nameof(Stream))]
			init
			{
				filePath = value;
				string extension = Path.GetExtension(filePath);
				Stream = extension switch
				{
					".ogg" => new VorbisWaveReader(filePath),
					".mp3" => new Mp3FileReader(filePath),
					".wav" or ".wave" => new WaveFileReader(filePath),
					".aiff" or ".aif" => new AiffFileReader(filePath),
					_ => (WaveStream)System.IO.Stream.Null,
				};
			}
		}
		internal RDWaveFile() { }
		public static RDWaveFile? Load(string path) =>
			Path.Exists(path)
				? new() { FilePath = path }
				: null;
		void IAssetFile.Save(string filepath) { }
		void IAssetFile.Save() { }
		public WaveStream Stream { get; private set; }
		public override string ToString() => Path.GetFileName(FilePath);
	}
}