using RhythmBase.Global.Components.Vector;
using RhythmBase.Global.Extensions;
using RhythmBase.Addition.Serialization;
using RhythmBase.Global.Serialization;
using RhythmBase.Global.Settings;
using RhythmBase.RhythmDoctor.Assets;
using RhythmBase.RhythmDoctor.Extensions;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace RhythmBase.RhythmDoctor.Serialization;

[JsonConverterFor(typeof(Sprite))]
internal sealed class SpriteConverter : MetadataJsonConverter<Sprite>
{
	public SpriteReadOrWriteSettings Settings { get; init; } = new();

	public override Sprite? Read(ref Utf8JsonReader reader, Type typeToConvert, MetadataJsonSerializerOptions options)
	{
		JsonException.ThrowIfNotMatch(ref reader, JsonTokenType.StartObject);
		Sprite sprite = new();
		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			JsonException.ThrowIfNotMatch(ref reader, JsonTokenType.PropertyName);
			if (reader.ValueTextEquals("name"u8) && reader.Read())
				sprite.Name = reader.GetString()!;
			else if (reader.ValueTextEquals("voice"u8) && reader.Read())
				sprite.Voice = reader.GetString()!;
			else if (reader.ValueTextEquals("size"u8) && reader.Read())
				sprite.Size = TypeConverterRegistry.Read<SizeNI>(ref reader, options);
			else if (reader.ValueTextEquals("rowPreviewOffset"u8) && reader.Read())
				sprite.RowPreviewOffset = TypeConverterRegistry.Read<PointNI>(ref reader, options);
			else if (reader.ValueTextEquals("rowPreviewFrame"u8) && reader.Read())
				sprite.RowPreviewFrame = reader.GetUInt32();
			else if (reader.ValueTextEquals("pivotOffset"u8) && reader.Read())
				sprite.PivotOffset = TypeConverterRegistry.Read<PointN>(ref reader, options);
			else if (reader.ValueTextEquals("portraitOffset"u8) && reader.Read())
				sprite.PortraitOffset = TypeConverterRegistry.Read<PointN>(ref reader, options);
			else if (reader.ValueTextEquals("portraitSize"u8) && reader.Read())
				sprite.PortraitSize = TypeConverterRegistry.Read<SizeNI>(ref reader, options);
			else if (reader.ValueTextEquals("portraitScale"u8) && reader.Read())
				sprite.PortraitScale = reader.GetSingle();
			else if (reader.ValueTextEquals("clips"u8) && reader.Read())
			{
				JsonException.ThrowIfNotMatch(ref reader, JsonTokenType.StartArray);
				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
					sprite.Clips.Add(ReadExpression(ref reader, options));
			}
			else
				reader.Skip();
		}
		return sprite;
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
		writer.WritePropertyName("size"u8);
		TypeConverterRegistry.Write(writer, value.Size, options);
		if (value.RowPreviewOffset is PointN p1)
			TypeConverterRegistry.Write(writer, "rowPreviewOffset"u8, p1, options);
		if (value.RowPreviewFrame is uint f1)
			writer.WriteNumber("rowPreviewFrame", f1);
		if (value.PivotOffset is PointN p2)
			TypeConverterRegistry.Write(writer, "pivotOffset"u8, p2, options);
		if (value.PortraitOffset is PointN p3)
			TypeConverterRegistry.Write(writer, "portraitOffset"u8, p3, options);
		if (value.PortraitSize is SizeNI s1)
			TypeConverterRegistry.Write(writer, "portraitSize"u8, s1, options);
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
					TypeConverterRegistry.Write(writer, "pivotOffset"u8, p4, options);
				if (clip.PortraitOffset is PointN p5)
					TypeConverterRegistry.Write(writer, "portraitOffset"u8, p5, options);
				if (clip.PortraitScale is float sc2)
					writer.WriteNumber("portraitScale", sc2);
				if (clip.PortraitSize is SizeNI s2)
					TypeConverterRegistry.Write(writer, "portraitSize"u8, s2, options);
				writer.WriteEndObject();
			});
		writer.WriteEndArray();
		writer.WriteEndObject();
	}

	private Sprite.Expression ReadExpression(ref Utf8JsonReader reader, MetadataJsonSerializerOptions options)
	{
		JsonException.ThrowIfNotMatch(ref reader, JsonTokenType.StartObject);
		Sprite.Expression clip = new() { Name = "" };
		while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
		{
			JsonException.ThrowIfNotMatch(ref reader, JsonTokenType.PropertyName);
			if (reader.ValueTextEquals("name"u8) && reader.Read())
				clip.Name = Settings.UppercasePrefixCharInExpressionNames
					? reader.GetString()!.WithoutUppercasePrefix()
					: reader.GetString()!;
			else if (reader.ValueTextEquals("frames"u8) && reader.Read())
			{
				JsonException.ThrowIfNotMatch(ref reader, JsonTokenType.StartArray);
				while (reader.Read() && reader.TokenType != JsonTokenType.EndArray)
					clip.Frames.Add(reader.GetInt32());
			}
			else if (reader.ValueTextEquals("loop"u8) && reader.Read())
				clip.Loop = EnumConverter.TryParse(ref reader, out LoopOption lv) ? lv : default;
			else if (reader.ValueTextEquals("loopStart"u8) && reader.Read())
				clip.LoopStart = reader.GetInt32();
			else if (reader.ValueTextEquals("fps"u8) && reader.Read())
				clip.Fps = reader.GetInt32();
			else if (reader.ValueTextEquals("reflectionOffset"u8) && reader.Read())
				clip.ReflectionOffset = reader.GetSingle();
			else if (reader.ValueTextEquals("pivotOffset"u8) && reader.Read())
				clip.PivotOffset = TypeConverterRegistry.Read<PointN>(ref reader, options);
			else if (reader.ValueTextEquals("portraitOffset"u8) && reader.Read())
				clip.PortraitOffset = TypeConverterRegistry.Read<PointN>(ref reader, options);
			else if (reader.ValueTextEquals("portraitScale"u8) && reader.Read())
				clip.PortraitScale = reader.GetSingle();
			else if (reader.ValueTextEquals("portraitSize"u8) && reader.Read())
				clip.PortraitSize = TypeConverterRegistry.Read<SizeNI>(ref reader, options);
			else
				reader.Skip();
		}
		return clip;
	}
}
