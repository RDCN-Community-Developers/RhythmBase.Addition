using RhythmBase.Global.Components.Vector;
using RhythmBase.RhythmDoctor.Assets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RhythmBase.RhythmDoctor.Converters
{
	internal class SpriteConverter : JsonConverter<RDSprite>
	{
		public override RDSprite? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
				throw new JsonException();
			RDSprite sprite = new();
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
						sprite.Size = new RDSizeNI(w, h);
						break;
					case "rowPreviewOffset":
						ReadPair(ref reader, out float rx, out float ry);
						sprite.RowPreviewOffset = new RDPointN(rx, ry);
						break;
					case "rowPreviewFrame":
						sprite.RowPreviewFrame = reader.GetUInt32();
						break;
					case "pivotOffset":
						ReadPair(ref reader, out float px, out float py);
						sprite.PivotOffset = new RDPointN(px, py);
						break;
					case "portraitOffset":
						ReadPair(ref reader, out float pox, out float poy);
						sprite.PortraitOffset = new RDPointN(pox, poy);
						break;
					case "portraitSize":
						ReadPair(ref reader, out int psw, out int psh);
						sprite.PortraitSize = new RDSizeNI(psw, psh);
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
							var clip = new RDSprite.Expression() { Name = "" };
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
										clip.Loop = Enum.Parse<LoopOption>(reader.GetString()!);
										break;
									case "loopStart":
										clip.LoopStart = reader.GetInt32();
										break;
									case "fps":
										clip.Fps = reader.GetInt32();
										break;
									case "pivotOffset":
										ReadPair(ref reader, out float cpx, out float cpy);
										clip.PivotOffset = new RDPointN(cpx, cpy);
										break;
									case "portraitOffset":
										ReadPair(ref reader, out float cpox, out float cpoy);
										clip.PivotOffset = new RDPointN(cpox, cpoy);
										break;
									case "portraitScale":
										clip.PortraitScale = reader.GetSingle();
										break;
									case "portraitSize":
										ReadPair(ref reader, out int cpsw, out int cpsh);
										clip.PortraitSize = new RDSizeNI(cpsw, cpsh);
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
		public override void Write(Utf8JsonWriter writer, RDSprite value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			if (!string.IsNullOrEmpty(value.DisplayName))
				writer.WriteString("displayName", value.DisplayName);
			if (!string.IsNullOrEmpty(value.Name))
				writer.WriteString("name", value.Name);
			if (!string.IsNullOrEmpty(value.Voice))
				writer.WriteString("voice", value.Voice);
			WritePair(writer, "size", value.Size.Width, value.Size.Height);
			if (value.RowPreviewOffset is RDPointN p1)
				WritePair(writer, "rowPreviewOffset", p1.X, p1.Y);
			if (value.RowPreviewFrame is uint f1)
				writer.WriteNumber("rowPreviewFrame", f1);
			if (value.PivotOffset is RDPointN p2)
				WritePair(writer, "pivotOffset", p2.X, p2.Y);
			if (value.PortraitOffset is RDPointN p3)
				WritePair(writer, "portraitOffset", p3.X, p3.Y);
			if (value.PortraitSize is RDSizeNI s1)
				WritePair(writer, "portraitSize", s1.Width, s1.Height);
			if (value.PortraitScale is float sc1)
				writer.WriteNumber("portraitScale", sc1);
			Dictionary<string, int> propertyNameLength = [];
			Dictionary<string, string[]> propertyStringValue = [];
			RDSprite.Expression[] array = [.. value.Clips];
			for (int i = 0; i < array.Length; i++)
			{
				RDSprite.Expression? clip = array[i];
				propertyNameLength["name"] = int.Max(propertyNameLength.GetValueOrDefault("name", 0), clip.Name.Length + 2);
				if (!propertyStringValue.TryGetValue("name", out var arr1))
					propertyStringValue["name"] = arr1 = new string[array.Length];
				arr1[i] = $"\"{clip.Name}\"";
				propertyNameLength["frames"] = int.Max(propertyNameLength.GetValueOrDefault("frames", 0), clip.Frames.Sum(i => i.ToString().Length) + clip.Frames.Count + 1);
				if (!propertyStringValue.TryGetValue("frames", out var arr2))
					propertyStringValue["frames"] = arr2 = new string[array.Length];
				arr2[i] = $"[{string.Join(',', clip.Frames)}]";
				propertyNameLength["loop"] = int.Max(propertyNameLength.GetValueOrDefault("loop", 0), clip.Loop.ToString().Length + 2);
				if (!propertyStringValue.TryGetValue("loop", out var arr3))
					propertyStringValue["loop"] = arr3 = new string[array.Length];
				arr3[i] = $"\"{clip.Loop.ToString()}\"";
				if (clip.LoopStart is int l)
				{
					propertyNameLength["loopStart"] = int.Max(propertyNameLength.GetValueOrDefault("loopStart", 0), l.ToString().Length);
					if (!propertyStringValue.TryGetValue("loopStart", out var arr4))
						propertyStringValue["loopStart"] = arr4 = new string[array.Length];
					arr4[i] = l.ToString();
				}
				propertyNameLength["fps"] = int.Max(propertyNameLength.GetValueOrDefault("fps", 0), clip.Fps.ToString().Length);
				if (!propertyStringValue.TryGetValue("fps", out var arr5))
					propertyStringValue["fps"] = arr5 = new string[array.Length];
				arr5[i] = clip.Fps.ToString();
				if (clip.PivotOffset is RDPointN p4)
				{
					propertyNameLength["pivotOffset"] = int.Max(propertyNameLength.GetValueOrDefault("pivotOffset", -1), p4.ToString().Length + 3);
					if (!propertyStringValue.TryGetValue("pivotOffset", out var arr6))
						propertyStringValue["pivotOffset"] = arr6 = new string[array.Length];
					arr6[i] = $"[{p4.X},{p4.Y}]";
				}
				if (clip.PortraitOffset is RDPointN p5)
				{
					propertyNameLength["portraitOffset"] = int.Max(propertyNameLength.GetValueOrDefault("portraitOffset", -1), p5.ToString().Length + 3);
					if (!propertyStringValue.TryGetValue("portraitOffset", out var arr7))
						propertyStringValue["portraitOffset"] = arr7 = new string[array.Length];
					arr7[i] = $"[{p5.X},{p5.Y}]";
				}
				if (clip.PortraitScale is float sc2)
				{
					propertyNameLength["portraitScale"] = int.Max(propertyNameLength.GetValueOrDefault("portraitScale", -1), sc2.ToString().Length);
					if (!propertyStringValue.TryGetValue("portraitScale", out var arr8))
						propertyStringValue["portraitScale"] = arr8 = new string[array.Length];
					arr8[i] = sc2.ToString();
				}
				if (clip.PortraitSize is RDSizeNI s2)
				{
					propertyNameLength["portraitSize"] = int.Max(propertyNameLength.GetValueOrDefault("portraitSize", -1), s2.ToString().Length + 3);
					if (!propertyStringValue.TryGetValue("portraitSize", out var arr9))
						propertyStringValue["portraitSize"] = arr9 = new string[array.Length];
					arr9[i] = $"[{s2.Width},{s2.Height}]";
				}
			}
			writer.WriteStartArray("clips");
			using MemoryStream stream = new();
			using Utf8JsonWriter writer1 = new(stream);
			string indent = new(options.IndentCharacter, writer.CurrentDepth * options.IndentSize);
			for (int i = 0; i < array.Length; i++)
			{
				writer1.WriteStartObject();
				foreach (var kvp in propertyStringValue)
				{
					if (!propertyNameLength.TryGetValue(kvp.Key, out int v) || v < 0)
						continue;
					writer1.WritePropertyName(kvp.Key);
					writer1.WriteRawValue(kvp.Value[i] + new string(' ', propertyNameLength[kvp.Key] - kvp.Value[i].Length), false);
				}
				writer1.WriteEndObject();
				writer1.Flush();
				writer.WriteRawValue("\n" + indent + Encoding.UTF8.GetString(stream.ToArray()[..(int)stream.Position]), true);
				stream.Position = 0;
				writer1.Reset();
			}
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
