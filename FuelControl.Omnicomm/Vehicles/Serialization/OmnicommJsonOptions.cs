using System.Text.Json;

namespace FuelControl.Omnicomm.Vehicles.Serialization;

public static class OmnicommJsonOptions
{
    public static JsonSerializerOptions Create()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(
            new OmnicommObjectConverter());

        options.Converters.Add(
            new OmnicommGroupConverter());

        return options;
    }
}