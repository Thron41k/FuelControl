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

        var root =
            profile.RootElement;

        if (!TryGetBoolean(
                root,
                "success",
                out var success) ||
            !success)
        {
            throw new InvalidOperationException(
                "Omnicomm не подтвердил успешную загрузку профиля.");
        }

        var calibrationTables =
            GetRequiredProperty(
                root,
                "copsProfile",
                "thing",
                "calibrationTables");

        if (calibrationTables.ValueKind !=
            JsonValueKind.Array)
        {
            throw new JsonException(
                "'calibrationTables' должен быть массивом.");
        }

        var result =
            new List<LldCalibrationTable>();

        foreach (var tableElement in
                 calibrationTables.EnumerateArray())
        {
            if (tableElement.ValueKind !=
                JsonValueKind.Object)
            {
                continue;
            }

            result.Add(
                ParseTable(tableElement));
        }

        return result;
    }

    private static LldCalibrationTable ParseTable(
        JsonElement element)
    {
        return new LldCalibrationTable
        {
            SensorNumber =
                ParseInt(
                    element,
                    "sensorNmb"),

            TerminalId =
                ParseLong(
                    element,
                    "terminalId"),

            TankNumber =
                ParseInt(
                    element,
                    "tankNmb"),

            MultiTankNumber =
                ParseInt(
                    element,
                    "multiTankNmb"),

            Records =
                ParseRecords(
                    GetRequiredProperty(
                        element,
                        "records"))
        };
    }

    private static IReadOnlyList<LldCalibrationRecord> ParseRecords(
        JsonElement recordsElement)
    {
        if (recordsElement.ValueKind !=
            JsonValueKind.Array)
        {
            throw new JsonException(
                "'records' в LLD таблице должен быть массивом.");
        }

        var records =
            new List<LldCalibrationRecord>();

        foreach (var recordElement in
                 recordsElement.EnumerateArray())
        {
            if (recordElement.ValueKind !=
                JsonValueKind.Object)
            {
                continue;
            }

            var code =
                ParseInt(
                    recordElement,
                    "code");

            var liters =
                ParseDecimal(
                    recordElement,
                    "liters");

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
            GetRequiredProperty(
                element,
                propertyName);

        return value.ValueKind switch
        {
            JsonValueKind.Number =>
                value.GetInt32(),

            JsonValueKind.String =>
                int.Parse(
                    value.GetString()!,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture),

            _ =>
                throw new JsonException(
                    $"Свойство '{propertyName}' " +
                    "имеет неверный формат.")
        };
    }

    private static long ParseLong(
        JsonElement element,
        string propertyName)
    {
        var value =
            GetRequiredProperty(
                element,
                propertyName);

        return value.ValueKind switch
        {
            JsonValueKind.Number =>
                value.GetInt64(),

            JsonValueKind.String =>
                long.Parse(
                    value.GetString()!,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture),

            _ =>
                throw new JsonException(
                    $"Свойство '{propertyName}' " +
                    "имеет неверный формат.")
        };
    }

    private static decimal ParseDecimal(
        JsonElement element,
        string propertyName)
    {
        var value =
            GetRequiredProperty(
                element,
                propertyName);

        return value.ValueKind switch
        {
            JsonValueKind.Number =>
                value.GetDecimal(),

            JsonValueKind.String =>
                decimal.Parse(
                    value.GetString()!,
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture),

            _ =>
                throw new JsonException(
                    $"Свойство '{propertyName}' " +
                    "имеет неверный формат.")
        };
    }

    private static bool TryGetBoolean(
        JsonElement element,
        string propertyName,
        out bool value)
    {
        value = false;

        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            return false;
        }

        if (property.ValueKind ==
            JsonValueKind.True)
        {
            value = true;
            return true;
        }

        if (property.ValueKind ==
            JsonValueKind.False)
        {
            return true;
        }

        if (property.ValueKind ==
            JsonValueKind.String &&
            bool.TryParse(
                property.GetString(),
                out value))
        {
            return true;
        }

        return false;
    }

    private static JsonElement GetRequiredProperty(
        JsonElement element,
        string propertyName)
    {
        if (!element.TryGetProperty(
                propertyName,
                out var property))
        {
            throw new JsonException(
                $"В профиле Omnicomm отсутствует " +
                $"свойство '{propertyName}'.");
        }

        return property;
    }

    private static JsonElement GetRequiredProperty(
        JsonElement root,
        params string[] path)
    {
        var current = root;

        foreach (var propertyName in path)
        {
            if (current.ValueKind !=
                JsonValueKind.Object ||
                !current.TryGetProperty(
                    propertyName,
                    out current))
            {
                throw new JsonException(
                    $"В профиле Omnicomm отсутствует " +
                    $"путь '{string.Join(".", path)}'.");
            }
        }

        return current;
    }
}