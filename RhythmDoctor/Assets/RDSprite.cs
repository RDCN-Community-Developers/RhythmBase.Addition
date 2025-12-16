using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using RhythmBase.Global.Assets;
using RhythmBase.Global.Components;
using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Extensions;
using RhythmBase.Global.Settings;
using RhythmBase.RhythmDoctor.Converters;
using SkiaSharp;
using System.Buffers;
using System.Text.Json;
namespace RhythmBase.RhythmDoctor.Assets
{
	/// <summary>
	/// A reference to an asset file.
	/// </summary>
	public class RDSprite : IAssetFile<RDSprite>
	{
		public string DisplayName => Path.GetFileName(FilePath);
		/// <inheritdoc/>
		[JsonIgnore]
		public string FilePath { get; init; } = string.Empty;
		/// <summary>
		/// The expression names of the sprite file.
		/// </summary>
		[JsonIgnore]
		public IEnumerable<string> Expressions => Clips.Select((i) => i.Name);
		/// <summary>
		/// The area where the sprite is previewed.
		/// </summary>
		[JsonIgnore]
		public SKRect? Preview => new SKRect?(RowPreviewFrame == null ? default(SKRect) : GetFrameRect(checked((int)RowPreviewFrame.Value)));
		/// <summary>
		/// The size of the sprite/image
		/// </summary>
		[JsonIgnore]
		public RDSizeNI ImageSize => new(ImageBase.Width, ImageBase.Height);
		/// <summary>
		/// Base layer
		/// </summary>
		[JsonIgnore]
		public SKBitmap? ImageBase { get; set; }
		/// <summary>
		/// Glow layer
		/// </summary>
		[JsonIgnore]
		public SKBitmap? ImageGlow { get; set; }
		/// <summary>
		/// Outline layer
		/// </summary>
		[JsonIgnore]
		public SKBitmap? ImageOutline { get; set; }
		/// <summary>
		/// Freeze layer
		/// </summary>
		[JsonIgnore]
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
		public RDSizeNI Size { get; set; }
		/// <summary>
		/// Information of expressions.
		/// </summary>
		public HashSet<Expression> Clips { get; set; } = new();
		/// <summary>
		/// Image offset when the row is previewed.
		/// </summary>
		public RDPointN? RowPreviewOffset { get; set; }
		/// <summary>
		/// Row preview frame.
		/// </summary>
		public uint? RowPreviewFrame { get; set; }
		/// <summary>
		/// Pivot point offset.
		/// </summary>
		public RDPointN? PivotOffset { get; set; }
		/// <summary>
		/// Image offset in dialog box.
		/// </summary>
		public RDPointN? PortraitOffset { get; set; }
		/// <summary>
		/// Image clipping in the dialog box.
		/// </summary>
		public RDSizeNI? PortraitSize { get; set; }
		/// <summary>
		/// Image scale in the dialog box.
		/// </summary>
		public float? PortraitScale { get; set; }
		public RDSprite()
		{
		}
		/// <summary>
		/// Create a reference to the file. The contents of the file are not read.
		/// </summary>
		/// <param name="filename">File path.</param>
		public RDSprite(string filename)
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
		public static RDSprite? FromFile(string path)
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
		public static RDSprite? FromStream(Stream stream, SpriteReadOrWriteSettings? settings = null)
		{
			SpriteConverter converter = new();
			byte[] buffer = new byte[stream.Length];
			stream.Read(buffer, 0, buffer.Length);
			var reader = new Utf8JsonReader(buffer, new()
			{
				AllowTrailingCommas = true
			});
			reader.Read();
			return converter.Read(ref reader, typeof(RDSprite), new());
		}
		/// <summary>
		/// Write JSON data to the text stream.
		/// </summary>
		/// <param name="textWriter">Text writer stream.</param>
		/// <param name="setting">Write settings.</param>
		public void WriteJson(Stream stream, SpriteReadOrWriteSettings? setting = null)
		{
			SpriteConverter converter = new();
			using var writer = new Utf8JsonWriter(stream, new()
			{
				SkipValidation = true,
				Indented = true,
			});
			converter.Write(writer, this, new());
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
			return GetFrameRect(checked((uint)index), ImageSize.ToSKSize(), Size.ToSKSize());
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
		public Expression AddBlankExpression(string name)
		{
			Expression C = Clips.FirstOrDefault(i => i.Name == name, new Expression
			{
				Name = name
			});
			Clips.Add(C);
			return C;
		}
		/// <summary>
		/// Add a blank emoticon to the creation of character assets.
		/// </summary>
		/// <returns>Added expressions. Further changes can be made on top of these.</returns>
		public IEnumerable<Expression> AddBlankExpressionsForCharacter() => from n in characterExpressionNames select AddBlankExpression(n);
		/// <summary>
		/// Add a blank emoticon to the creation of sprite assets.
		/// </summary>
		/// <returns>Added expression. Further changes can be made on top of this.</returns>
		public IEnumerable<Expression> AddBlankExpressionForDecoration() => [AddBlankExpression("neutral")];
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
			[JsonConverter(typeof(StringEnumConverter))]
			public LoopOption Loop { get; set; }
			/// <summary>
			/// The frame rate of the emoticon when <c>loop == yes</c>.
			/// </summary>
			public float Fps { get; set; }
			/// <summary>
			/// Pivot point offset.
			/// </summary>
			public RDPointN? PivotOffset { get; set; }
			/// <summary>
			/// Image offset in dialog box.
			/// </summary>
			public RDPointN? PortraitOffset { get; set; }
			/// <summary>
			/// Image scale in the dialog box.
			/// </summary>
			public float? PortraitScale { get; set; }
			/// <summary>
			/// Image clipping in the dialog box.
			/// </summary>
			public RDSizeNI? PortraitSize { get; set; }
			public override string ToString() => Name;
		}
	}
}