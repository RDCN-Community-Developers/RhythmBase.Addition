using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using RhythmBase.Global.Assets;
using RhythmBase.Global.Components;
using RhythmBase.Global.Extensions;
using RhythmBase.Global.Settings;
using RhythmBase.RhythmDoctor.Extensions;
using SkiaSharp;
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
		public required SKBitmap ImageBase { get; set; }
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
		public HashSet<Expression> Clips { get; set; } = [];
		/// <summary>
		/// Image offset when the row is previewed.
		/// </summary>
		public RDSizeN? RowPreviewOffset { get; set; }
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
		public RDSizeN? PortraitOffset { get; set; }
		/// <summary>
		/// Image clipping in the dialog box.
		/// </summary>
		public RDSizeN? PortraitSize { get; set; }
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
		public static RDSprite? Load(string path)
		{
			string _file = Path.Combine(Path.GetDirectoryName(path) ?? "", Path.GetFileNameWithoutExtension(path));
			JsonSerializer setting = new();
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
			JObject obj = setting.Deserialize<JObject>(new JsonTextReader(File.OpenText($"{json}.json")))!;
			string imageBaseFile = $"{json}.png";
			string imageGlowFile = $"{json}_glow.png";
			string imageOutlineFile = $"{json}_outline.png";
			string imageFreezeFile = $"{json}_freeze.png";
			if (File.Exists(imageBaseFile))
			{
				RDSprite sprite = new()
				{
					FilePath = path,
					ImageBase = SKBitmap.Decode(imageBaseFile),
					ImageGlow = File.Exists(imageGlowFile) ? SKBitmap.Decode(imageGlowFile) : null,
					ImageOutline = File.Exists(imageOutlineFile) ? SKBitmap.Decode(imageOutlineFile) : null,
					ImageFreeze = File.Exists(imageFreezeFile) ? SKBitmap.Decode(imageFreezeFile) : null,
					Name = obj[nameof(Name).ToLowerCamelCase()]?.ToObject<string>(),
					Voice = obj[nameof(Voice).ToLowerCamelCase()]?.ToObject<string>(),
					Size = obj[nameof(Size).ToLowerCamelCase()]!.ToObject<RDSizeNI>(),
					RowPreviewOffset = obj[nameof(RowPreviewOffset).ToLowerCamelCase()]?.ToObject<RDSizeN>(),
					RowPreviewFrame = obj[nameof(RowPreviewFrame).ToLowerCamelCase()]?.ToObject<uint>(),
					PivotOffset = obj[nameof(PivotOffset).ToLowerCamelCase()]?.ToObject<RDPointN>(),
					PortraitOffset = obj[nameof(PortraitOffset).ToLowerCamelCase()]?.ToObject<RDSizeN>(),
					PortraitSize = obj[nameof(PortraitSize).ToLowerCamelCase()]?.ToObject<RDSizeN>(),
					PortraitScale = obj[nameof(PortraitScale).ToLowerCamelCase()]?.ToObject<float>()
				};
				foreach (JToken clip in obj[nameof(Clips).ToLowerCamelCase()] ?? new JObject())
					sprite.Clips.Add(clip.ToObject<Expression>()!);
				return sprite;
			}
			return null;
		}
		public void Save() => Save(FilePath);
		/// <summary>
		/// Write JSON data to the text stream.
		/// </summary>
		/// <param name="textWriter">Text writer stream.</param>
		public void WriteJson(TextWriter textWriter) => WriteJson(textWriter, new SpriteReadOrWriteSettings());
		/// <summary>
		/// Write JSON data to the text stream.
		/// </summary>
		/// <param name="textWriter">Text writer stream.</param>
		/// <param name="setting">Write settings.</param>
		public void WriteJson(TextWriter textWriter, SpriteReadOrWriteSettings setting)
		{
			JsonSerializerSettings jsonS = new()
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver(),
				NullValueHandling = NullValueHandling.Ignore,
				Formatting = Formatting.None
			};
			JsonTextWriter writer = new(textWriter)
			{
				Formatting = setting.Indented ? Formatting.Indented : Formatting.None
			};
			JObject meObj = JObject.FromObject(this, JsonSerializer.Create(jsonS));
			JArray? clipArray = (JArray?)meObj["Clips".ToLowerCamelCase()];
			Dictionary<string, int> PropertyNameLength = [];
			Dictionary<string, List<string>> propertyValues = [];
			if (clipArray is not null)
				foreach (JToken jtoken in clipArray)
				{
					JObject clip = (JObject)jtoken;
					foreach (KeyValuePair<string, JToken?> pair in clip)
					{
						string stringedValue = pair.Value?.Type == JTokenType.Null ? string.Empty : JsonConvert.SerializeObject(pair.Value, Formatting.None, jsonS);
						if (propertyValues.TryGetValue(pair.Key, out var value))
							value.Add(stringedValue);
						else
							propertyValues[pair.Key] = [stringedValue];
						if (PropertyNameLength.TryGetValue(pair.Key, out int value2))
							PropertyNameLength[pair.Key] = Math.Max(value2, stringedValue.Length);
						else
							PropertyNameLength[pair.Key] = stringedValue.Length;
					}
				}
			if (!setting.IgnoreNullValue)
				foreach (KeyValuePair<string, List<string>> pair2 in propertyValues)
					if (pair2.Value.Contains(string.Empty) &&
						pair2.Value.Any(i => i != string.Empty))
						PropertyNameLength[pair2.Key] = Math.Max(PropertyNameLength[pair2.Key], 4);
			meObj.Remove("Size".ToLowerCamelCase());
			meObj.Remove("Clips".ToLowerCamelCase());
			JsonTextWriter jsonTextWriter = writer;
			jsonTextWriter.WriteStartObject();
			foreach (KeyValuePair<string, JToken?> pair3 in meObj)
			{
				if (pair3.Value?.Type != JTokenType.Null)
				{
					jsonTextWriter.WritePropertyName(pair3.Key);
					jsonTextWriter.WriteRawValue(JsonConvert.SerializeObject(pair3.Value, Formatting.None, jsonS));
				}
			}
			jsonTextWriter.WritePropertyName("Size".ToLowerCamelCase());
			jsonTextWriter.WriteStartArray();
			jsonTextWriter.WriteValue(Size.Width);
			jsonTextWriter.WriteValue(Size.Height);
			jsonTextWriter.WriteEndArray();
			jsonTextWriter.WritePropertyName("Clips".ToLowerCamelCase());
			jsonTextWriter.WriteStartArray();
			int num = (clipArray?.Count ?? 0) - 1;
			for (int j = 0; j <= num; j++)
			{
				jsonTextWriter.WriteStartObject();
				writer.Formatting = Formatting.None;
				foreach (KeyValuePair<string, List<string>> pair4 in propertyValues)
				{
					if (PropertyNameLength[pair4.Key] > 0)
					{
						if (setting.IgnoreNullValue)
						{
							if (string.IsNullOrEmpty(pair4.Value[j]))
							{
								if (setting.Indented)
								{
									jsonTextWriter.WriteWhitespace(string.Empty.PadRight(pair4.Key.Length + PropertyNameLength[pair4.Key] + 4));
								}
							}
							else
							{
								jsonTextWriter.WritePropertyName(pair4.Key);
								jsonTextWriter.WriteRawValue(pair4.Value[j].PadRight(setting.Indented ? PropertyNameLength[pair4.Key] : 0));
							}
						}
						else
						{
							jsonTextWriter.WritePropertyName(pair4.Key);
							if (string.IsNullOrEmpty(pair4.Value[j]))
							{
								jsonTextWriter.WriteRawValue(JsonConvert.Null.PadRight(setting.Indented ? PropertyNameLength[pair4.Key] : 0));
							}
							else
							{
								jsonTextWriter.WriteRawValue(pair4.Value[j].PadRight(setting.Indented ? PropertyNameLength[pair4.Key] : 0));
							}
						}
					}
				}
				jsonTextWriter.WriteEndObject();
				writer.Formatting = setting.Indented ? Formatting.Indented : Formatting.None;
			}
			jsonTextWriter.WriteEndArray();
			jsonTextWriter.WriteEndObject();
			textWriter.Flush();
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
																  select GetFrameRect((int)i)).ToArray();
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
		public void Save(string path, SpriteReadOrWriteSettings settings)
		{
			FileInfo file = new(path);
			string WithoutExtension = Path.Combine(file.Directory?.FullName ?? "", Path.GetFileNameWithoutExtension(file.Name));
			if (settings.WithImage)
			{
				ImageBase.Save(WithoutExtension + ".png");
				ImageGlow?.Save(WithoutExtension + "_glow.png");
				ImageOutline?.Save(WithoutExtension + "_outline.png");
				ImageFreeze?.Save(WithoutExtension + "_freeze.png");
			}
			using StreamWriter stream = new FileInfo(WithoutExtension + ".json").CreateText();
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
			public List<int> Frames { get; set; } = [];
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
			public RDSizeN? PortraitOffset { get; set; }
			/// <summary>
			/// Image scale in the dialog box.
			/// </summary>
			public float? PortraitScale { get; set; }
			/// <summary>
			/// Image clipping in the dialog box.
			/// </summary>
			public RDSizeN? PortraitSize { get; set; }
			public override string ToString() => Name;
		}
	}
}