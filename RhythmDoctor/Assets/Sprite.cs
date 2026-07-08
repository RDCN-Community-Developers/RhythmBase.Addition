using RhythmBase.Global.Assets;
using RhythmBase.Global.Components;
using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Extensions;
using RhythmBase.Global.Settings;
using RhythmBase.RhythmDoctor.Serialization;
using SkiaSharp;
using System.Buffers;
using System.Text.Json;
namespace RhythmBase.RhythmDoctor.Assets
{
	/// <summary>
	/// A reference to an asset file.
	/// </summary>
	public class Sprite : IAssetFile<Sprite>
	{
		public string DisplayName => Path.GetFileName(FilePath);
		/// <inheritdoc/>
		public string FilePath { get; init; } = string.Empty;
		/// <summary>
		/// The expression names of the sprite file.
		/// </summary>
		public IEnumerable<string> Expressions => Clips.Select((i) => i.Name);
		/// <summary>
		/// The area where the sprite is previewed.
		/// </summary>
		public SKRect? Preview => new SKRect?(RowPreviewFrame == null ? default(SKRect) : GetFrameRect(checked((int)RowPreviewFrame.Value)));
		/// <summary>
		/// The size of the sprite/image
		/// </summary>
		public SizeNI ImageSize => ImageBase is null ? default : new(ImageBase.Width, ImageBase.Height);
		/// <summary>
		/// Base layer
		/// </summary>
		public SKBitmap? ImageBase { get; set; }
		/// <summary>
		/// Glow layer
		/// </summary>
		public SKBitmap? ImageGlow { get; set; }
		/// <summary>
		/// Outline layer
		/// </summary>
		public SKBitmap? ImageOutline { get; set; }
		/// <summary>
		/// Freeze layer
		/// </summary>
		public SKBitmap? ImageFreeze { get; set; }
		/// <summary>
		/// The name of the sprite.
		/// </summary>
		public string? Name { get; set; }
		/// <summary>
		/// [Unknown] The voice of the sprite.
		/// </summary>
		public string? Voice { get; set; }
		/// <summary>
		/// The size of each expression.
		/// </summary>
		public SizeNI Size { get; set; }
		/// <summary>
		/// Information of expressions.
		/// </summary>
		public HashSet<Expression> Clips { get; set; } = new();
		/// <summary>
		/// Image offset when the row is previewed.
		/// </summary>
		public PointN? RowPreviewOffset { get; set; }
		/// <summary>
		/// Row preview frame.
		/// </summary>
		public uint? RowPreviewFrame { get; set; }
		/// <summary>
		/// Pivot point offset.
		/// </summary>
		public PointN? PivotOffset { get; set; }
		/// <summary>
		/// Image offset in dialog box.
		/// </summary>
		public PointN? PortraitOffset { get; set; }
		/// <summary>
		/// Image clipping in the dialog box.
		/// </summary>
		public SizeNI? PortraitSize { get; set; }
		/// <summary>
		/// Image scale in the dialog box.
		/// </summary>
		public float? PortraitScale { get; set; }
		public Sprite()
		{
		}
		/// <summary>
		/// Create a reference to the file. The contents of the file are not read.
		/// </summary>
		/// <param name="filename">File path.</param>
		public Sprite(string filename)
		{
			if (string.IsNullOrEmpty(filename))
			{
				throw new ArgumentException("Filename cannot be null.", nameof(filename));
			}
			FilePath = filename;
		}
		/// <summary>
		/// Load the file contents into memory.
		/// </summary>
		public static Sprite? FromFile(string path)
		{
			string _file = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path));
			bool flag = File.Exists($"{_file}.json");
			string json;
			if (flag)
			{
				json = $"{_file}";
			}
			else
			{
				if (!File.Exists($"{_file}\\{Path.GetFileName(_file)}.json"))
					return null;
				json = $"{_file}\\{Path.GetFileName(_file)}";
			}
			using FileStream stream = File.OpenRead($"{json}.json");
			return FromStream(stream, null);
		}
		public static Sprite? FromStream(Stream stream, SpriteReadOrWriteSettings? settings = null)
		{
			SpriteConverter converter = new();
			byte[] buffer = new byte[stream.Length];
			stream.Read(buffer, 0, buffer.Length);
			var reader = new Utf8JsonReader(buffer, new()
			{
				AllowTrailingCommas = true
			});
			reader.Read();
			return converter.Read(ref reader, typeof(Sprite), new() { JsonSerializerOptions = new() });
		}
		/// <summary>
		/// Write JSON data to the text stream.
		/// </summary>
		/// <param name="textWriter">Text writer stream.</param>
		/// <param name="setting">Write settings.</param>
		public void WriteJson(Stream stream, SpriteReadOrWriteSettings? setting = null)
		{
			SpriteConverter converter = new() { Settings = setting ?? new(), };
			using var writer = new Utf8JsonWriter(stream, new()
			{
				SkipValidation = true,
				Indented = true,
			});
			converter.Write(writer, this, new() { JsonSerializerOptions = new() });
			writer.Flush();
		}
		/// <summary>
		/// Gets the frame crop area.
		/// </summary>
		/// <param name="index">Frame index.</param>
		/// <returns>A rectangular area that indicates the cropping area.</returns>
		public SKRectI GetFrameRect(int index)
		{
			if (index < 0)
			{
				throw new OverflowException();
			}
			return GetFrameRect(checked((uint)index), ImageSize.ToSKSizeI(), Size.ToSKSizeI());
		}
		private static SKRectI GetFrameRect(uint index, SKSizeI source, SKSizeI size)
		{
			int column = source.Width / size.Width;
			SKPointI leftTop = new(
					(int)(index % column * size.Width),
					(int)(index / column * size.Height));
			return new(leftTop.X,
								 leftTop.Y,
								 leftTop.X + size.Width,
								 leftTop.Y + size.Height);
		}
		/// <summary>
		/// Get the cropped area on the image for each frame of this expression.
		/// </summary>
		/// <returns>An array of rectangles indicating each crop area.</returns>
		public SKRectI[] GetFrameRects(Expression expression) => (from i in expression.Frames
																															select GetFrameRect(i)).ToArray();
		/// <summary>
		/// Add a blank expression.
		/// </summary>
		/// <param name="name">Expression name.</param>
		/// <returns>Added expression. Further changes can be made on top of this.</returns>
		public Expression AddBlankExpression(string name, params int[] indices)
		{
			Expression C = Clips.FirstOrDefault(i => i.Name == name, new Expression
			{
				Name = name,
				Frames = indices.ToCircularList(),
			});
			Clips.Add(C);
			return C;
		}
		/// <summary>
		/// Add a blank emoticon to the creation of character assets.
		/// </summary>
		/// <returns>Added expressions. Further changes can be made on top of these.</returns>
		public IEnumerable<Expression> AddBlankExpressionsForCharacter(params int[] indices) => from n in characterExpressionNames select AddBlankExpression(n, indices);
		/// <summary>
		/// Add a blank emoticon to the creation of sprite assets.
		/// </summary>
		/// <returns>Added expression. Further changes can be made on top of this.</returns>
		public IEnumerable<Expression> AddBlankExpressionForDecoration(params int[] indices) => [AddBlankExpression("neutral", indices)];
		/// <summary>
		/// Save the file.
		/// </summary>
		/// <param name="path">the file path.</param>
		/// <exception cref="T:RhythmBase.Exceptions.OverwriteNotAllowedException">The save path is the same as the reference path.</exception>
		public void Save(string path) => Save(path, new SpriteReadOrWriteSettings());
		/// <summary>
		/// Save the file.
		/// </summary>
		/// <param name="path">the file path.</param>
		/// <param name="settings">save settings.</param>
		/// <exception cref="T:RhythmBase.Exceptions.OverwriteNotAllowedException">The save path is the same as the reference path.</exception>
		public void Save(string path, SpriteReadOrWriteSettings? settings = null)
		{
			settings ??= new();
			FileInfo file = new(path);
			string WithoutExtension = Path.Combine(file.Directory?.FullName ?? "", Path.GetFileNameWithoutExtension(file.Name));
			if (settings.WithImage)
			{
				ImageBase?.Save(WithoutExtension + ".png");
				ImageGlow?.Save(WithoutExtension + "_glow.png");
				ImageOutline?.Save(WithoutExtension + "_outline.png");
				ImageFreeze?.Save(WithoutExtension + "_freeze.png");
			}
			using FileStream stream = File.Open(WithoutExtension + ".json", FileMode.Create, FileAccess.Write);
			WriteJson(stream, settings);
		}
		/// <inheritdoc/>
		public override string ToString() => string.IsNullOrEmpty(Name) ? DisplayName : Name;
		private static readonly string[] characterExpressionNames =
			[
				"neutral",
				"happy",
				"barely",
				"missed"
			];
		/// <summary>
		/// An expression.
		/// </summary>
		public class Expression
		{
			/// <summary>
			/// Expression name.
			/// </summary>
			public required string Name { get; set; }
			/// <summary>
			/// The list of frame indexes for expression.
			/// </summary>
			public CircularList<int> Frames { get; set; } = [];
			/// <summary>
			/// The start frame of the cycle for the expression.
			/// </summary>
			public int? LoopStart { get; set; }
			/// <summary>
			/// The way the expression loops.
			/// </summary>
			public LoopOption Loop { get; set; }
			/// <summary>
			/// The frame rate of the emoticon when <c>loop == yes</c>.
			/// </summary>
			public float Fps { get; set; }
			public float ReflectionOffset { get; set; }
			/// <summary>
			/// Pivot point offset.
			/// </summary>
			public PointN? PivotOffset { get; set; }
			/// <summary>
			/// Image offset in dialog box.
			/// </summary>
			public PointN? PortraitOffset { get; set; }
			/// <summary>
			/// Image scale in the dialog box.
			/// </summary>
			public float? PortraitScale { get; set; }
			/// <summary>
			/// Image clipping in the dialog box.
			/// </summary>
			public SizeNI? PortraitSize { get; set; }
			public override string ToString() => Name;
		}
	}
}