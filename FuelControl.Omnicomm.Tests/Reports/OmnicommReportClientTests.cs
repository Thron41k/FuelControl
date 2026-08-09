// FuelControl.Omnicomm.Tests/Reports/OmnicommReportClientTests.cs
using System.Net;
using System.Text.Json;
using FuelControl.Omnicomm.Reports;
using FuelControl.Omnicomm.Tests.Fakes;
using NUnit.Framework;

namespace FuelControl.Omnicomm.Tests.Reports;

[TestFixture]
public sealed class OmnicommReportClientTests
{
    private static string GetDeliveryReportJson()
    {
        var filePath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "ответ_delivery_report.json");

        return File.ReadAllText(filePath);
    }

    [Test]
    public async Task GetDeliveryReportAsync_ShouldReturnMappedEvents()
    {
        // Arrange
        var json = GetDeliveryReportJson();

        var fakeApi = new FakeOmnicommApiClient(_ =>
            FakeOmnicommApiClient.JsonResponse(json));

        var client = new OmnicommReportClient(fakeApi);

        var vehicleIds = new long[] { 303013526 };
        var from = DateTimeOffset.FromUnixTimeMilliseconds(1786201200000);
        var to = DateTimeOffset.FromUnixTimeMilliseconds(1786287540000);

        // Act
        var report = await client.GetDeliveryReportAsync(
            vehicleIds,
            from,
            to,
            timeZone: "Asia/Chita");

        // Assert
        Assert.That(report, Is.Not.Null);
        Assert.That(report.Status, Is.EqualTo("SUCCESS"));
        Assert.That(
            report.ReportId,
            Is.EqualTo("8616c56f-5e8c-4c8c-b811-84d95ad8cadc"));
        Assert.That(report.Events, Is.Not.Empty);

        var first = report.Events[0];
        Assert.That(first.Id, Is.EqualTo(1));
        Assert.That(first.VehicleId, Is.EqualTo(303013526));
        Assert.That(
            first.Name,
            Is.EqualTo("Топливозаправщик КамАЗ К 129 ЕВ 2"));
        Assert.That(first.VolumeLiters, Is.EqualTo(199.83m));
        Assert.That(
            first.StartDate,
            Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1786228019000)));
        Assert.That(
            first.EndDate,
            Is.EqualTo(DateTimeOffset.FromUnixTimeMilliseconds(1786228169000)));
    }

    [Test]
    public async Task GetDeliveryReportAsync_ShouldSendCorrectRequest()
    {
        // Arrange
        var json = GetDeliveryReportJson();

        var fakeApi = new FakeOmnicommApiClient(_ =>
            FakeOmnicommApiClient.JsonResponse(json));

        var client = new OmnicommReportClient(fakeApi);

        var vehicleIds = new long[] { 303013526, 111222333 };
        var from = DateTimeOffset.FromUnixTimeMilliseconds(1786201200000);
        var to = DateTimeOffset.FromUnixTimeMilliseconds(1786287540000);

        // Act
        await client.GetDeliveryReportAsync(
            vehicleIds,
            from,
            to,
            timeZone: "Asia/Chita");

        // Assert
        Assert.That(fakeApi.LastRequest, Is.Not.Null);
        Assert.That(
            fakeApi.LastRequest!.Method,
            Is.EqualTo(HttpMethod.Post));
        Assert.That(
            fakeApi.LastRequest.RequestUri!.ToString(),
            Does.Contain("/service/reports/"));

        Assert.That(fakeApi.LastRequestBody, Is.Not.Null.And.Not.Empty);

        using var document = JsonDocument.Parse(fakeApi.LastRequestBody!);
        var root = document.RootElement;

        Assert.That(
            root.GetProperty("type").GetString(),
            Is.EqualTo("ASEReport"));

        Assert.That(
            root.GetProperty("tz").GetString(),
            Is.EqualTo("Asia/Chita"));

        var paramsElement = root.GetProperty("params");
        var innerParams = paramsElement.GetProperty("params");

        Assert.That(
            innerParams.GetProperty("action").GetString(),
            Is.EqualTo("getReportDataDlv"));

        Assert.That(
            innerParams.GetProperty("rows").GetInt32(),
            Is.EqualTo(500));

        Assert.That(
            paramsElement.GetProperty("url").GetString(),
            Is.EqualTo("/delivery"));

        var vehicleIdArray = innerParams.GetProperty("vehicleID");
        Assert.That(vehicleIdArray.GetArrayLength(), Is.EqualTo(2));
        Assert.That(vehicleIdArray[0].GetInt64(), Is.EqualTo(303013526));
        Assert.That(vehicleIdArray[1].GetInt64(), Is.EqualTo(111222333));
    }

    [Test]
    public void GetDeliveryReportAsync_WhenVehicleIdsEmpty_ShouldThrow()
    {
        // Arrange
        var fakeApi = new FakeOmnicommApiClient(_ =>
            FakeOmnicommApiClient.JsonResponse("{}"));

        var client = new OmnicommReportClient(fakeApi);

        // Act + Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.GetDeliveryReportAsync(
                Array.Empty<long>(),
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow));
    }

    [Test]
    public void GetDeliveryReportAsync_WhenToLessOrEqualFrom_ShouldThrow()
    {
        // Arrange
        var fakeApi = new FakeOmnicommApiClient(_ =>
            FakeOmnicommApiClient.JsonResponse("{}"));

        var client = new OmnicommReportClient(fakeApi);

        var now = DateTimeOffset.UtcNow;

        // Act + Assert
        Assert.ThrowsAsync<ArgumentException>(async () =>
            await client.GetDeliveryReportAsync(
                new long[] { 1 },
                now,
                now));
    }

    [Test]
    public void GetDeliveryReportAsync_WhenStatusNotSuccess_ShouldThrow()
    {
        // Arrange
        var errorJson = """
            {
              "id": "test-id",
              "status": "ERROR",
              "results": null
            }
            """;

        var fakeApi = new FakeOmnicommApiClient(_ =>
            FakeOmnicommApiClient.JsonResponse(errorJson));

        var client = new OmnicommReportClient(fakeApi);

        // Act + Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await client.GetDeliveryReportAsync(
                new long[] { 303013526 },
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow));

        Assert.That(ex!.Message, Does.Contain("ERROR"));
    }

    [Test]
    public void GetDeliveryReportAsync_WhenHttpError_ShouldThrow()
    {
        // Arrange
        var fakeApi = new FakeOmnicommApiClient(_ =>
            FakeOmnicommApiClient.JsonResponse(
                "Internal Server Error",
                HttpStatusCode.InternalServerError));

        var client = new OmnicommReportClient(fakeApi);

        // Act + Assert
        Assert.ThrowsAsync<HttpRequestException>(async () =>
            await client.GetDeliveryReportAsync(
                new long[] { 303013526 },
                DateTimeOffset.UtcNow.AddDays(-1),
                DateTimeOffset.UtcNow));
    }
}