using System.Text.Json;
using System.Text.Json.Serialization;
using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Omnicomm.Vehicles.Serialization;

public sealed class OmnicommVehicleGroupConverter
    : JsonConverter<OmnicommVehicleGroup>
{
    public override OmnicommVehicleGroup Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(
                "√руппа Omnicomm должна быть JSON-массивом.");
        }

        using var document =
            JsonDocument.ParseValue(ref reader);

        var array = document.RootElement;

        if (array.GetArrayLength() != 4)
        {
            throw new JsonException(
                "ќжидалось 4 элемента в группе Omnicomm.");
        }

        var id = array[0].GetInt64();

        var name = array[1].GetString() ?? string.Empty;

        var type = array[2].GetString() ?? string.Empty;

        var data = array[3];

        var objectIds = ReadLongArray(
            data,
            "objects");

        var childGroupIds = ReadLongArray(
            data,
            "groups");

        return new OmnicommVehicleGroup
        {
            Id = id,
            Name = name,
            Type = type,
            ObjectIds = objectIds,
            ChildGroupIds = childGroupIds
        };
    }

    public override void Write(
        Utf8JsonWriter writer,
        OmnicommVehicleGroup value,
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

    private static IReadOnlyList<long> ReadLongArray(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return Array.Empty<long>();
        }

        return property
            .EnumerateArray()
            .Select(x => x.GetInt64())
            .ToArray();
    }
}