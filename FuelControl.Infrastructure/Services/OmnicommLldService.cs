using System.Globalization;
using System.Text.Json;
using FuelControl.Infrastructure.Services.Interfaces;
using FuelControl.Omnicomm.Tools.Lld.Models;
using FuelControl.Omnicomm.Vehicles;

namespace FuelControl.Infrastructure.Services;

public sealed class OmnicommLldService(
    IOmnicommVehicleProfileClient profileClient)
    : IOmnicommLldService
{
    public async Task<IReadOnlyList<LldCalibrationTable>> GetTablesAsync(
        long omnicommObjectId,
        CancellationToken cancellationToken = default)
    {
        var profile =
            await profileClient.GetAsync(
                omnicommObjectId,
                cancellationToken);

        if (profile is null)
        {
            throw new InvalidOperationException(
                "Omnicomm вернул пустой профиль.");
        }

        if (!profile.RootElement.TryGetProperty(
                "success",
                out var successElement) ||
            !successElement.GetBoolean())
        {
            throw new InvalidOperationException(
                "Omnicomm не подтвердил успешную загрузку профиля.");
        }

        var calibrationTables =
            profile.RootElement
                .GetProperty("copsProfile")
                .GetProperty("thing")
                .GetProperty("calibrationTables");

        var result =
            new List<LldCalibrationTable>();

        foreach (var tableElement in calibrationTables.EnumerateArray())
        {
            var records =
                ParseRecords(
                    tableElement.GetProperty("records"));

            result.Add(
                new LldCalibrationTable
                {
                    SensorNumber =
                        ParseInt(
                            tableElement,
                            "sensorNmb"),

                    TerminalId =
                        ParseLong(
                            tableElement,
                            "terminalId"),

                    TankNumber =
                        ParseInt(
                            tableElement,
                            "tankNmb"),

                    MultiTankNumber =
                        ParseInt(
                            tableElement,
                            "multiTankNmb"),

                    Records = records
                });
        }

        return result;
    }

    private static IReadOnlyList<LldCalibrationRecord> ParseRecords(
        JsonElement recordsElement)
    {
        var records = new List<LldCalibrationRecord>();

        foreach (var property in recordsElement.EnumerateObject())
        {
            if (!int.TryParse(
                    property.Name,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out var code))
            {
                continue;
            }

            var liters =
                property.Value.ValueKind switch
                {
                    JsonValueKind.Number =>
                        property.Value.GetDecimal(),

                    JsonValueKind.String =>
                        decimal.Parse(
                            property.Value.GetString()!,
                            CultureInfo.InvariantCulture),

                    _ =>
                        throw new JsonException(
                            $"Некорректное значение LLD для кода {code}.")
                };

            records.Add(
                new LldCalibrationRecord(
                    code,
                    liters));
        }

        return records
            .OrderBy(x => x.Code)
            .ToArray();
    }

    private static int ParseInt(
        JsonElement element,
        string propertyName)
    {
        var value =
            element.GetProperty(propertyName);

        return value.ValueKind switch
        {
            JsonValueKind.Number =>
                value.GetInt32(),

            JsonValueKind.String =>
                int.Parse(
                    value.GetString()!,
                    CultureInfo.InvariantCulture),

            _ =>
                throw new JsonException(
                    $"Свойство '{propertyName}' имеет неверный формат.")
        };
    }

    private static long ParseLong(
        JsonElement element,
        string propertyName)
    {
        var value =
            element.GetProperty(propertyName);

        return value.ValueKind switch
        {
            JsonValueKind.Number =>
                value.GetInt64(),

            JsonValueKind.String =>
                long.Parse(
                    value.GetString()!,
                    CultureInfo.InvariantCulture),

            _ =>
                throw new JsonException(
                    $"Свойство '{propertyName}' имеет неверный формат.")
        };
    }
}