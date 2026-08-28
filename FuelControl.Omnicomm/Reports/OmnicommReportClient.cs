// FuelControl.Omnicomm/Reports/OmnicommReportClient.cs
using FuelControl.Omnicomm.Http;
using FuelControl.Omnicomm.Reports.Models;
using FuelControl.Omnicomm.Reports.Models.Internal;
using System.Net.Http.Json;
using System.Text.Json;

namespace FuelControl.Omnicomm.Reports;

public sealed class OmnicommReportClient(
    IOmnicommApiClient apiClient) : IOmnicommReportClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters =
        {
            new OmnicommSpeedPointDtoConverter(),
            new OmnicommFuelLevelPointDtoConverter()
        }
    };

    public async Task<OmnicommDeliveryReport> GetDeliveryReportAsync(
        IReadOnlyList<long> vehicleIds,
        DateTimeOffset from,
        DateTimeOffset to,
        OmnicommTimeZone timeZone,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vehicleIds);
        ArgumentNullException.ThrowIfNull(timeZone);

        if (vehicleIds.Count == 0)
            throw new ArgumentException(
                "Необходимо указать хотя бы один vehicleId.",
                nameof(vehicleIds));

        if (to <= from)
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.");

        var fromMs = from.ToUnixTimeMilliseconds();
        var toMs = to.ToUnixTimeMilliseconds();

        var requestBody = new
        {
            @params = new
            {
                from = fromMs,
                to = toMs,
                @params = new
                {
                    winterOffset = timeZone.WinterOffset,
                    summerOffset = timeZone.SummerOffset,
                    page = 1,
                    rows = 500,
                    from = fromMs,
                    to = toMs,
                    action = "getReportDataDlv",
                    newui = true,
                    locale = "ru",
                    reportFromdate = fromMs,
                    fromDatetime = fromMs,
                    reportTodate = toMs,
                    toDatetime = toMs,
                    selectedRoots = new[] { "FTC" },
                    ID = vehicleIds,
                    vehicleID = vehicleIds,
                    tz = timeZone,
                    objectType = new[] { "FTC" },
                    objectClass = new[] { 1 },
                    service = false
                },
                url = "/delivery",
                method = "POST",
                traditional = true
            },
            meta = new
            {
                report = "delivery",
                vehiclesCount = vehicleIds.Count
            },
            tz = timeZone.TimeZone,
            type = "ASEReport",
            rebuild = true,
            service = false,
            sync = 1
        };

        using var response = await apiClient.SendAsync(
            () =>
            {
                var message = new HttpRequestMessage(
                    HttpMethod.Post,
                    "/service/reports/");

                message.Content = JsonContent.Create(
                    requestBody,
                    options: JsonOptions);

                return message;
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        var raw = JsonSerializer.Deserialize<OmnicommDeliveryReportResponse>(
            json,
            JsonOptions);

        if (raw is null)
        {
            throw new InvalidOperationException(
                "Omnicomm вернул пустой ответ отчёта delivery.");
        }

        if (!string.Equals(raw.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Omnicomm вернул статус отчёта: {raw.Status}");
        }

        var events = (raw.Results?.Rows ?? [])
            .Select(row => new OmnicommDeliveryEvent
            {
                Id = row.Id,
                VehicleId = row.Vehicleid,
                Name = row.Name,
                StartDate = DateTimeOffset.FromUnixTimeMilliseconds(row.Startdate),
                EndDate = DateTimeOffset.FromUnixTimeMilliseconds(row.Enddate),
                VolumeLiters = row.Volume / 100m
            })
            .ToList();

        return new OmnicommDeliveryReport
        {
            ReportId = raw.Id,
            Status = raw.Status,
            TotalRecords = raw.Results?.Records ?? events.Count,
            Events = events
        };
    }

    public async Task<OmnicommFuelEventsReport> GetFuelEventsReportAsync(
    IReadOnlyList<long> vehicleIds,
    DateTimeOffset from,
    DateTimeOffset to,
    OmnicommTimeZone timeZone,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vehicleIds);

        if (vehicleIds.Count == 0)
            throw new ArgumentException(
                "Необходимо указать хотя бы один vehicleId.",
                nameof(vehicleIds));

        if (to <= from)
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.");

        var fromMs = from.ToUnixTimeMilliseconds();
        var toMs = to.ToUnixTimeMilliseconds();

        var requestBody = new
        {
            type = "ASEReport",
            sync = 1,
            rebuild = true,
            tz = timeZone.TimeZone,
            meta = new
            {
                report = "fueleventsreport",
                vehiclesCount = vehicleIds.Count
            },
            @params = new
            {
                from = fromMs,
                to = toMs,
                @params = new
                {
                    winterOffset = timeZone.WinterOffset,
                    summerOffset = timeZone.SummerOffset,
                    action = "getReportData",
                    newui = true,
                    locale = "ru",
                    reportFromdate = fromMs,
                    fromDatetime = fromMs,
                    reportTodate = toMs,
                    toDatetime = toMs,
                    selectedRoots = new[] { "FAS" },
                    ID = vehicleIds,
                    vehicleID = vehicleIds,
                    tz = timeZone.TimeZone,
                    objectType = new[] { "FAS" },
                    objectClass = new[] { 1 },
                    rows = 500,
                    page = 1,
                    sidx = "startDate",
                    sord = "asc"
                },
                url = "/fueleventsreport",
                method = "POST",
                traditional = true
            }
        };

        using var response = await apiClient.SendAsync(
            () =>
            {
                var message = new HttpRequestMessage(
                    HttpMethod.Post,
                    "/service/reports/");

                message.Content = JsonContent.Create(
                    requestBody,
                    options: JsonOptions);

                return message;
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);

        var raw = JsonSerializer.Deserialize<OmnicommFuelEventsReportResponse>(
            json,
            JsonOptions);

        if (raw is null)
        {
            throw new InvalidOperationException(
                "Omnicomm вернул пустой ответ отчёта fueleventsreport.");
        }

        if (!string.Equals(raw.Status, "SUCCESS", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Omnicomm вернул статус отчёта: {raw.Status}");
        }

        var events = (raw.Results?.Rows ?? [])
            .Select(MapFuelEvent)
            .ToList();

        return new OmnicommFuelEventsReport
        {
            ReportId = raw.Id,
            Status = raw.Status,
            TotalRecords = raw.Results?.Records ?? events.Count,
            Events = events
        };
    }

    private static OmnicommFuelEvent MapFuelEvent(
        OmnicommFuelEventRowDto row)
    {
        double? longitude = null;
        double? latitude = null;

        if (row.Coordinates is { Length: >= 2 })
        {
            longitude = row.Coordinates[0];
            latitude = row.Coordinates[1];
        }

        return new OmnicommFuelEvent
        {
            Id = row.Id,
            VehicleId = row.VehicleID,
            Name = row.Name,
            Type = MapFuelEventType(row.Type),

            VolumeLiters =
                row.Volume / 10m,

            StartDate =
                DateTimeOffset.FromUnixTimeMilliseconds(
                    row.Startdate),

            EndDate =
                DateTimeOffset.FromUnixTimeMilliseconds(
                    row.Enddate),

            EventDate =
                DateTimeOffset.FromUnixTimeMilliseconds(
                    row.Eventdate),

            TankNumber = row.TankNmb,

            Longitude = longitude,
            Latitude = latitude,

            Address = row.Address,

            DriverId = row.DriverID,
            DriverName = row.Driver,

            IsFtc = row.IsFTC,
            IsLls5 = row.IsLLS5,
            Exclusion = row.Exclusion
        };
    }

    private static OmnicommFuelEventType MapFuelEventType(int type) =>
        type switch
        {
            1 => OmnicommFuelEventType.Refuel,
            3 => OmnicommFuelEventType.Drain,
            _ => OmnicommFuelEventType.Unknown
        };

    public async Task<OmnicommSpeedReport> GetSpeedReportAsync(
    IReadOnlyList<long> vehicleIds,
    DateTimeOffset from,
    DateTimeOffset to,
    OmnicommTimeZone timeZone,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vehicleIds);

        if (vehicleIds.Count == 0)
        {
            throw new ArgumentException(
                "Необходимо указать хотя бы один vehicleId.",
                nameof(vehicleIds));
        }

        if (to <= from)
        {
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.");
        }

        var fromMs = from.ToUnixTimeMilliseconds();
        var toMs = to.ToUnixTimeMilliseconds();

        var requestBody = new
        {
            type = "ASEReport",
            sync = 1,
            rebuild = true,
            tz = timeZone.TimeZone,

            meta = new
            {
                report = "speed",
                vehiclesCount = vehicleIds.Count
            },

            @params = new
            {
                from = fromMs,
                to = toMs,

                @params = new
                {
                    winterOffset = timeZone.WinterOffset,
                    summerOffset = timeZone.SummerOffset,

                    action = "getReportData",
                    newui = true,

                    locale = "ru",

                    reportFromdate = fromMs,
                    fromDatetime = fromMs,

                    reportTodate = toMs,
                    toDatetime = toMs,

                    selectedRoots = new[]
                    {
                    "FTC"
                },

                    ID = vehicleIds,

                    vehicleID = vehicleIds,

                    tz = timeZone.TimeZone,

                    objectType = new[]
                    {
                    "FTC"
                },

                    objectClass = new[]
                    {
                    1
                },

                    maxPoints = 300
                },

                url = "speed",

                method = "POST",

                traditional = true
            }
        };

        using var response = await apiClient.SendAsync(
            () =>
            {
                var message = new HttpRequestMessage(
                    HttpMethod.Post,
                    "/service/reports/");

                message.Content = JsonContent.Create(
                    requestBody,
                    options: JsonOptions);

                return message;
            },
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(
            cancellationToken);

        var raw = JsonSerializer.Deserialize<OmnicommSpeedReportResponse>(
            json,
            JsonOptions);

        if (raw is null)
        {
            throw new InvalidOperationException(
                "Omnicomm вернул пустой ответ отчёта speed.");
        }

        if (!string.Equals(
                raw.Status,
                "SUCCESS",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Omnicomm вернул статус отчёта: {raw.Status}");
        }

        var points = (raw.Results?.SpeedData ?? [])
            .Select(point => new OmnicommSpeedPoint
            {
                Timestamp =
                    DateTimeOffset.FromUnixTimeMilliseconds(
                        point.Timestamp),

                SpeedKmh = point.Speed
            })
            .OrderBy(x => x.Timestamp)
            .GroupBy(x => x.Timestamp)
            .Select(g => new OmnicommSpeedPoint
            {
                Timestamp = g.Key,

                /*
                 * Если Omnicomm прислал несколько значений
                 * с одинаковым timestamp, оставляем максимальное.
                 *
                 * Это безопаснее для контроля движения:
                 * если на одном timestamp были разные показания,
                 * не потеряем потенциальное превышение скорости.
                 */
                SpeedKmh = g.Max(x => x.SpeedKmh)
            })
            .ToList();

        return new OmnicommSpeedReport
        {
            ReportId = raw.Id,
            Status = raw.Status,
            MaximalSpeedKmh = raw.Results?.MaximalSpeed ?? 0,
            Points = points
        };
    }

    public async Task<OmnicommFuelLevelReport> GetFuelLevelReportAsync(
    IReadOnlyList<long> vehicleIds,
    DateTimeOffset from,
    DateTimeOffset to,
    OmnicommTimeZone timeZone,
    CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(vehicleIds);
        ArgumentNullException.ThrowIfNull(timeZone);

        if (vehicleIds.Count == 0)
        {
            throw new ArgumentException(
                "Необходимо указать хотя бы один vehicleId.",
                nameof(vehicleIds));
        }

        if (to <= from)
        {
            throw new ArgumentException(
                "Дата окончания должна быть больше даты начала.");
        }

        var fromMs =
            from.ToUnixTimeMilliseconds();

        var toMs =
            to.ToUnixTimeMilliseconds();

        var requestBody = new
        {
            @params = new
            {
                from = fromMs,
                to = toMs,

                @params = new
                {
                    winterOffset =
                        timeZone.WinterOffset,

                    summerOffset =
                        timeZone.SummerOffset,

                    action = "getReportData",

                    newui = true,

                    locale = "ru",

                    reportFromdate = fromMs,

                    fromDatetime = fromMs,

                    reportTodate = toMs,

                    toDatetime = toMs,

                    selectedRoots = new[]
                    {
                    "FTC"
                },

                    ID = vehicleIds,

                    vehicleID = vehicleIds,

                    tz = timeZone.TimeZone,

                    objectType = new[]
                    {
                    "FTC"
                },

                    objectClass = new[]
                    {
                    1
                },

                    service = false,

                    maxPoints = 300
                },

                url = "/fuellevels",

                method = "POST",

                traditional = true
            },

            tz = timeZone.TimeZone,

            type = "ASEReport",

            service = false,

            sync = 1,

            rebuild = true
        };

        using var response =
            await apiClient.SendAsync(
                () =>
                {
                    var message =
                        new HttpRequestMessage(
                            HttpMethod.Post,
                            "/service/reports/");

                    message.Content =
                        JsonContent.Create(
                            requestBody,
                            options: JsonOptions);

                    return message;
                },
                cancellationToken);

        response.EnsureSuccessStatusCode();

        var json =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        var raw =
            JsonSerializer.Deserialize<
                OmnicommFuelLevelReportResponse>(
                    json,
                    JsonOptions);

        if (raw is null)
        {
            throw new InvalidOperationException(
                "Omnicomm вернул пустой ответ отчёта fuellevels.");
        }

        if (!string.Equals(
                raw.Status,
                "SUCCESS",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Omnicomm вернул статус отчёта: {raw.Status}");
        }

        var tanks =
            raw.Results?.Data
                .Select(MapFuelLevelTank)
                .ToList()
            ?? [];

        return new OmnicommFuelLevelReport
        {
            ReportId = raw.Id,

            Status = raw.Status,

            VehicleId =
                vehicleIds.Count == 1
                    ? vehicleIds[0]
                    : 0,

            TotalRecords =
                tanks.Sum(x => x.Points.Count),

            Tanks = tanks
        };
    }

    private static OmnicommFuelLevelTank MapFuelLevelTank(
        OmnicommFuelLevelDataDto data)
    {
        var rawValues =
            data.RawValues.ToDictionary(
                x => x.Timestamp,
                x => x.FuelLiters);

        var approxValues =
            data.ApproxValues.ToDictionary(
                x => x.Timestamp,
                x => x.FuelLiters);

        var timestamps =
            rawValues.Keys
                .Union(approxValues.Keys)
                .Order();

        var points =
            timestamps
                .Select(timestamp =>
                    new OmnicommFuelLevelPoint
                    {
                        Timestamp =
                            DateTimeOffset
                                .FromUnixTimeMilliseconds(
                                    timestamp),

                        RawLiters =
                            rawValues.TryGetValue(
                                timestamp,
                                out var raw)
                                ? raw
                                : null,

                        ApproxLiters =
                            approxValues.TryGetValue(
                                timestamp,
                                out var approx)
                                ? approx
                                : null
                    })
                .ToList();

        return new OmnicommFuelLevelTank
        {
            TankNumber = data.TankNumber,
            CapacityLiters = data.TankCapacity,
            Points = points
        };
    }
}

