using System.Text.Json;
using System.Text.Json.Serialization;
using FuelControl.Omnicomm.Vehicles.Models;

namespace FuelControl.Omnicomm.Vehicles.Serialization;

public sealed class OmnicommGroupConverter : JsonConverter<OmnicommGroup>
{
    public override OmnicommGroup Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        Console.WriteLine(
            ">>> OmnicommGroupConverter CALLED");
        if (reader.TokenType != JsonTokenType.StartArray)
        {
            throw new JsonException(
                $"Ожидался массив группы Omnicomm, " +
                $"получен {reader.TokenType}.");
        }

        using var document =
            JsonDocument.ParseValue(ref reader);

        var array = document.RootElement;

        if (array.ValueKind != JsonValueKind.Array ||
            array.GetArrayLength() < 4)
        {
            throw new JsonException(
                "Некорректный формат группы Omnicomm.");
        }

        var id = array[0].GetInt64();

        var name =
            array[1].GetString() ?? string.Empty;

        var type =
            array[2].GetString() ?? string.Empty;

        var data = array[3];

        var objectIds =
            data.TryGetProperty(
                "objects",
                out var objects)
                ? objects
                    .EnumerateArray()
                    .Select(x => x.GetInt64())
                    .ToArray()
                : [];

        var childGroupIds =
            data.TryGetProperty(
                "groups",
                out var groups)
                ? groups
                    .EnumerateArray()
                    .Select(x => x.GetInt64())
                    .ToArray()
                : [];

        return new OmnicommGroup
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
        OmnicommGroup value,
        JsonSerializerOptions options)
    {
        throw new NotSupportedException();
    }
}