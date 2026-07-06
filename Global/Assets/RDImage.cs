using RhythmBase.Global.Components;
using RhythmBase.Global.Extensions;
using RhythmBase.Global.Components.Vector;
using SkiaSharp;
using System.Diagnostics.CodeAnalysis;
namespace RhythmBase.Global.Assets
{
	public class RDImage : IAssetFile<RDImage>
	{
		public string DisplayName => Path.GetFileName(FilePath);
		private Lazy<SKBitmap> _image;
		private string filePath;

		public SKBitmap Image
		{
			get => _image.Value;
			set => _image = new(value);
		}
		/// <inheritdoc/>
		public SizeNI Size { get; private set; }
		/// <inheritdoc/>
		public required string FilePath
		{
			get => filePath;
			[MemberNotNull(nameof(filePath))]
			[MemberNotNull(nameof(_image))]
			init
			{
				filePath = value;
				_image = new(() => SKBitmap.Decode(filePath));
			}
		}
		/// <summary>
		/// Load the file contents into memory.
		/// </summary>
		public static RDImage? FromFile(string path)
		{
			if (!Path.Exists(path))
				return null;
			RDImage image = new()
			{
				FilePath = path
			};
			SKBitmap imgFile = SKBitmap.Decode(path);
			if (imgFile == null)
				return null;
			image.Image = imgFile;
			image.Size = new SizeNI(imgFile.Width, imgFile.Height);
			return image;
		}
		/// <inheritdoc/>
		public void Save(string filepath) => Image.Save(filepath);
		/// <inheritdoc/>
		public void Save() => Image.Save(FilePath);
		/// <inheritdoc/>
		public override string ToString() => Path.GetFileName(FilePath);
	}
}