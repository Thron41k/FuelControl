using System.Text.Json;
using System.Text.Json.Serialization;
using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Omnicomm.Vehicles.Serialization;

public sealed class OmnicommObjectConverter
    : JsonConverter<OmnicommObject>
{
    public override OmnicommObject Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(
                "Ожидался массив OmnicommObject.");
        }

        using var document =
            JsonDocument.ParseValue(ref reader);

        var array = document.RootElement;

        if (array.ValueKind != JsonValueKind.Array ||
            array.GetArrayLength() != 6)
        {
            throw new JsonException(
                "OmnicommObject должен содержать 6 элементов.");
        }

        return new OmnicommObject
        {
            Id = array[0].GetInt64(),

            Name = array[1].GetString()
                   ?? string.Empty,

            Type = array[2].GetString()
                   ?? string.Empty,

            SystemType = array[3].GetString()
                         ?? string.Empty,

            Value = array[4].GetInt32(),

            Flag = array[5].GetBoolean()
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OmnicommObject value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        writer.WriteNumberValue(value.Id);
        writer.WriteStringValue(value.Name);
        writer.WriteStringValue(value.Type);
        writer.WriteStringValue(value.SystemType);
        writer.WriteNumberValue(value.Value);
        writer.WriteBooleanValue(value.Flag);

        writer.WriteEndArray();
    }
}