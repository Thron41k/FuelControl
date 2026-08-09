using System.Text.Json;
using System.Text.Json.Serialization;
using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Omnicomm.Vehicles.Serialization;

public sealed class OmnicommGroupConverter
    : JsonConverter<OmnicommGroup>
{
    public override OmnicommGroup Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(
                "Ожидался массив OmnicommGroup.");
        }

        using var document =
            JsonDocument.ParseValue(ref reader);

        var array = document.RootElement;

        if (array.ValueKind != JsonValueKind.Array ||
            array.GetArrayLength() != 4)
        {
            throw new JsonException(
                "OmnicommGroup должен содержать 4 элемента.");
        }

        var details = array[3];

        return new OmnicommGroup
        {
            Id = array[0].GetInt64(),

            Name = array[1].GetString()
                ?? string.Empty,

            Type = array[2].GetString()
                ?? string.Empty,

            ObjectIds = ReadIds(
                details,
                "objects"),

            ChildGroupIds = ReadIds(
                details,
                "groups")
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OmnicommGroup value,
        JsonSerializerOptions options)
    {
        writer.WriteStartArray();

        writer.WriteNumberValue(value.Id);
        writer.WriteStringValue(value.Name);
        writer.WriteStringValue(value.Type);

        writer.WriteStartObject();

        writer.WritePropertyName("objects");

        writer.WriteStartArray();

        foreach (var objectId in value.ObjectIds)
        {
            writer.WriteNumberValue(objectId);
        }

        writer.WriteEndArray();

        writer.WritePropertyName("groups");

        writer.WriteStartArray();

        foreach (var groupId in value.ChildGroupIds)
        {
            writer.WriteNumberValue(groupId);
        }

        writer.WriteEndArray();

        writer.WriteEndObject();

        writer.WriteEndArray();
    }

    private static IReadOnlyList<long> ReadIds(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return [];
        }

        if (property.ValueKind != JsonValueKind.Array)
        {
            throw new JsonException(
                $"Поле '{propertyName}' должно быть массивом.");
        }

        return property
            .EnumerateArray()
            .Select(x => x.GetInt64())
            .ToArray();
    }
}