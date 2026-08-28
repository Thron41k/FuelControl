using System.Text.Json;
using System.Text.Json.Serialization;

namespace FuelControl.Omnicomm.Reports.Models.Internal;

internal sealed class OmnicommFuelLevelPointDtoConverter
    : JsonConverter<OmnicommFuelLevelPointDto>
{
    public override OmnicommFuelLevelPointDto Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(
                "Точка уровня топлива Omnicomm должна быть JSON-массивом.");
        }

        if (!reader.Read() ||
            reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException(
                "В точке уровня топлива отсутствует timestamp.");
        }

        var timestamp =
            reader.GetInt64();

        if (!reader.Read() ||
            reader.TokenType != JsonTokenType.Number)
        {
            throw new JsonException(
                "В точке уровня топлива отсутствует значение топлива.");
        }

        var fuelLiters =
            reader.GetDecimal();

        if (!reader.Read() ||
            reader.TokenType != JsonTokenType.EndArray)
        {
            throw new JsonException(
                "Некорректная структура точки уровня топлива.");
        }

        return new OmnicommFuelLevelPointDto
        {
            Timestamp = timestamp,
            FuelLiters = fuelLiters
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OmnicommFuelLevelPointDto value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        writer.WriteNumberValue(
            value.Timestamp);

        writer.WriteNumberValue(
            value.FuelLiters);

        writer.WriteEndArray();
    }
}