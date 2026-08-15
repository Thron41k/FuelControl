using System.Text.Json;
using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommSpeedPointDtoConverter
    : JsonConverter<OmnicommSpeedPointDto>
{
    public override OmnicommSpeedPointDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(
                "Точка скорости Omnicomm должна быть JSON-массивом.");
        }

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException(
                "В точке скорости отсутствует timestamp.");
        }

        var timestamp = reader.GetInt64();

        if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException(
                "В точке скорости отсутствует значение скорости.");
        }

        var speed = reader.GetDecimal();

        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException(
                "Некорректная структура точки скорости.");
        }

        return new OmnicommSpeedPointDto
        {
            Timestamp = timestamp,
            Speed = speed
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OmnicommSpeedPointDto value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.Timestamp);
        writer.WriteNumberValue(value.Speed);
        writer.WriteEndArray();
    }
}