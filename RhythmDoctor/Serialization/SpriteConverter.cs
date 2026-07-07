using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Extensions;
using RhythmBase.Global.Serialization;
using RhythmBase.Global.Settings;
using RhythmBase.RhythmDoctor.Assets;
using RhythmBase.RhythmDoctor.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RhythmBase.RhythmDoctor.Serialization
{
	internal class SpriteConverter : MetadataJsonConverter<Sprite>
	{
		public SpriteReadOrWriteSettings Settings { get; init; } = new();
		public override Sprite? Read(ref Utf8JsonReader reader, Type typeToConvert, MetadataJsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();
			Sprite sprite = new();
			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
					return sprite;
				if (reader.TokenType != JsonTokenType.PropertyName)
					throw new JsonException();
				string propertyName = reader.GetString()!;
				reader.Read();
				switch (propertyName)
				{
					case "name":
						sprite.Name = reader.GetString()!;
						break;
					case "voice":
						sprite.Voice = reader.GetString()!;
						break;
					case "size":
						ReadPair(ref reader, out int w, out int h);
						sprite.Size = new SizeNI(w, h);
						break;
					case "rowPreviewOffset":
						ReadPair(ref reader, out float rx, out float ry);
						sprite.RowPreviewOffset = new PointN(rx, ry);
						break;
					case "rowPreviewFrame":
						sprite.RowPreviewFrame = reader.GetUInt32();
						break;
					case "pivotOffset":
						ReadPair(ref reader, out float px, out float py);
						sprite.PivotOffset = new PointN(px, py);
						break;
					case "portraitOffset":
						ReadPair(ref reader, out float pox, out float poy);
						sprite.PortraitOffset = new PointN(pox, poy);
						break;
					case "portraitSize":
						ReadPair(ref reader, out int psw, out int psh);
						sprite.PortraitSize = new SizeNI(psw, psh);
						break;
					case "portraitScale":
						sprite.PortraitScale = reader.GetSingle();
						break;
					case "clips":
						if (reader.TokenType != JsonTokenType.StartArray)
							throw new JsonException();
						while (reader.Read())
						{
							if (reader.TokenType == JsonTokenType.EndArray)
								break;
							if (reader.TokenType != JsonTokenType.StartObject)
								throw new JsonException();
							var clip = new Sprite.Expression() { Name = "" };
							while (reader.Read())
							{
								if (reader.TokenType == JsonTokenType.EndObject)
									break;
								if (reader.TokenType != JsonTokenType.PropertyName)
									throw new JsonException();
								propertyName = reader.GetString()!;
								reader.Read();
								switch (propertyName)
								{
									case "name":
										if (Settings.UppercasePrefixCharInExpressionNames)
											clip.Name = reader.GetString()!.WithoutUppercasePrefix();
										else
											clip.Name = reader.GetString()!;
										break;
									case "frames":
										if (reader.TokenType != JsonTokenType.StartArray)
											throw new JsonException();
										while (reader.Read())
										{
											if (reader.TokenType == JsonTokenType.EndArray)
												break;
											if (reader.TokenType != JsonTokenType.Number)
												throw new JsonException();
											clip.Frames.Add(reader.GetInt32());
										}
										break;
									case "loop":
										string? v1 = reader.GetString();
										if (string.IsNullOrEmpty(v1)) break;
										v1 = v1.ToUpperCamelCase();
										clip.Loop = Enum.TryParse(v1, out LoopOption v2) ? v2 : default;
										break;
									case "loopStart":
										clip.LoopStart = reader.GetInt32();
										break;
									case "fps":
										clip.Fps = reader.GetInt32();
										break;
									case "reflectionOffset":
										clip.ReflectionOffset = reader.GetSingle();
										break;
									case "pivotOffset":
										ReadPair(ref reader, out float cpx, out float cpy);
										clip.PivotOffset = new PointN(cpx, cpy);
										break;
									case "portraitOffset":
										ReadPair(ref reader, out float cpox, out float cpoy);
										clip.PivotOffset = new PointN(cpox, cpoy);
										break;
									case "portraitScale":
										clip.PortraitScale = reader.GetSingle();
										break;
									case "portraitSize":
										ReadPair(ref reader, out int cpsw, out int cpsh);
										clip.PortraitSize = new SizeNI(cpsw, cpsh);
										break;
								}
							}
							sprite.Clips.Add(clip);
						}
						if (reader.TokenType != JsonTokenType.EndArray)
							throw new JsonException();
						break;
					default:
						throw new JsonException();
				}
			}
			throw new JsonException();
		}
		public override void Write(Utf8JsonWriter writer, Sprite value, MetadataJsonSerializerOptions options)
		{
			writer.WriteStartObject();
			if (!string.IsNullOrEmpty(value.DisplayName))
				writer.WriteString("displayName", value.DisplayName);
			if (!string.IsNullOrEmpty(value.Name))
				writer.WriteString("name", value.Name);
			if (!string.IsNullOrEmpty(value.Voice))
				writer.WriteString("voice", value.Voice);
			WritePair(writer, "size", value.Size.Width, value.Size.Height);
			if (value.RowPreviewOffset is PointN p1)
				WritePair(writer, "rowPreviewOffset", p1.X, p1.Y);
			if (value.RowPreviewFrame is uint f1)
				writer.WriteNumber("rowPreviewFrame", f1);
			if (value.PivotOffset is PointN p2)
				WritePair(writer, "pivotOffset", p2.X, p2.Y);
			if (value.PortraitOffset is PointN p3)
				WritePair(writer, "portraitOffset", p3.X, p3.Y);
			if (value.PortraitSize is SizeNI s1)
				WritePair(writer, "portraitSize", s1.Width, s1.Height);
			if (value.PortraitScale is float sc1)
				writer.WriteNumber("portraitScale", sc1);
			writer.WritePropertyName("clips");
			writer.WriteStartArray();
			using (NoIndentScope noIndentScope = new(options.JsonSerializerOptions.Encoder, options))
				noIndentScope.WriteNoIndentArrayTo(options, writer, value.Clips, (writer, clip, _) =>
				{
					writer.WriteStartObject();
					writer.WriteString("name", Settings.UppercasePrefixCharInExpressionNames
						? clip.Name.WithUppercasePrefix() : clip.Name);
					writer.WritePropertyName("frames");
					writer.WriteStartArray();
					foreach (int frame in clip.Frames)
						writer.WriteNumberValue(frame);
					writer.WriteEndArray();
					writer.WriteString("loop", clip.Loop.ToString().ToLowerCamelCase());
					if (clip.LoopStart is int l)
						writer.WriteNumber("loopStart", l);
					writer.WriteNumber("fps", clip.Fps);
					writer.WriteNumber("reflectionOffset", clip.ReflectionOffset);
					if (clip.PivotOffset is PointN p4)
					{
						writer.WritePropertyName("pivotOffset");
						writer.WriteStartArray();
						writer.WriteNumberValue(p4.X);
						writer.WriteNumberValue(p4.Y);
						writer.WriteEndArray();
					}
					if (clip.PortraitOffset is PointN p5)
					{
						writer.WritePropertyName("portraitOffset");
						writer.WriteStartArray();
						writer.WriteNumberValue(p5.X);
						writer.WriteNumberValue(p5.Y);
						writer.WriteEndArray();
					}
					if (clip.PortraitScale is float sc2)
						writer.WriteNumber("portraitScale", sc2);
					if (clip.PortraitSize is SizeNI s2)
					{
						writer.WritePropertyName("portraitSize");
						writer.WriteStartArray();
						writer.WriteNumberValue(s2.Width);
						writer.WriteNumberValue(s2.Height);
						writer.WriteEndArray();
					}
					writer.WriteEndObject();
				});
			writer.WriteEndArray();
			writer.WriteEndObject();
		}
		private static void WritePair(Utf8JsonWriter writer, string propertyName, int a, int b)
		{
			writer.WriteStartArray(propertyName);
			writer.WriteNumberValue(a);
			writer.WriteNumberValue(b);
			writer.WriteEndArray();
		}
		private static void WritePair(Utf8JsonWriter writer, string propertyName, float a, float b)
		{
			writer.WriteStartArray(propertyName);
			writer.WriteNumberValue(a);
			writer.WriteNumberValue(b);
			writer.WriteEndArray();
		}
		private static void ReadPair(ref Utf8JsonReader reader, out int a, out int b)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
				throw new JsonException();
			reader.Read();
			if (reader.TokenType != JsonTokenType.Number)
				throw new JsonException();
			a = reader.GetInt32();
			reader.Read();
			if (reader.TokenType != JsonTokenType.Number)
				throw new JsonException();
			b = reader.GetInt32();
			reader.Read();
			if (reader.TokenType != JsonTokenType.EndArray)
				throw new JsonException();
		}
		private static void ReadPair(ref Utf8JsonReader reader, out float a, out float b)
		{
			if (reader.TokenType != JsonTokenType.StartArray)
				throw new JsonException();
			reader.Read();
			if (reader.TokenType != JsonTokenType.Number)
				throw new JsonException();
			a = reader.GetSingle();
			reader.Read();
			if (reader.TokenType != JsonTokenType.Number)
				throw new JsonException();
			b = reader.GetSingle();
			reader.Read();
			if (reader.TokenType != JsonTokenType.EndArray)
				throw new JsonException();
		}
	}
}
