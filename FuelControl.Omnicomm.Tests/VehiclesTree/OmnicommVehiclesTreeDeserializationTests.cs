using FuelControl.Omnicomm.Models;
using FuelControl.Omnicomm.Vehicles.Serialization;
using System.Text.Json;

namespace FuelControl.Omnicomm.Tests.VehiclesTree;

[TestFixture]
public sealed class OmnicommVehiclesTreeDeserializationTests
{
    [Test]
    public async Task Deserialize_ResponseFile_ShouldSucceed()
    {
        // Arrange
        var filePath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "ответ_vehiclesTree.json");

        Assert.That(
            File.Exists(filePath),
            Is.True,
            $"Файл не найден: {filePath}");

        var json =
            await File.ReadAllTextAsync(filePath);

        var options =
            OmnicommJsonOptions.Create();

        // Act
        var result =
            JsonSerializer.Deserialize<
                OmnicommVehiclesTreeResponse>(
                json,
                options);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Objects, Is.Not.Null);
        Assert.That(result.Groups, Is.Not.Null);

        TestContext.WriteLine(
            $"Objects: {result.Objects.Count}");

        TestContext.WriteLine(
            $"Groups: {result.Groups.Count}");
    }

    [Test]
    public async Task Deserialize_Response_ShouldReadKamazAtz()
    {
        // Arrange
        var filePath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "ответ_vehiclesTree.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        var result =
            JsonSerializer.Deserialize<
                OmnicommVehiclesTreeResponse>(
                json,
                OmnicommJsonOptions.Create());

        // Act
        var vehicle =
            result!.Objects
                .SingleOrDefault(x =>
                    x.Id == 1187000337);

        // Assert
        Assert.That(vehicle, Is.Not.Null);

        Assert.That(
            vehicle!.Name,
            Is.EqualTo("КАМАЗ АТЗ  в548ва138"));

        Assert.That(
            vehicle.Type,
            Is.EqualTo("vehicle"));

        Assert.That(
            vehicle.SystemType,
            Is.EqualTo("FTC"));
    }

    [Test]
    public async Task Deserialize_Response_ShouldReadGroup()
    {
        // Arrange
        var filePath = Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "TestData",
            "ответ_vehiclesTree.json");

        var json =
            await File.ReadAllTextAsync(filePath);

        var result =
            JsonSerializer.Deserialize<
                OmnicommVehiclesTreeResponse>(
                json,
                OmnicommJsonOptions.Create());

        // Act
        var group =
            result!.Groups
                .SingleOrDefault(x =>
                    x.Id == 5126);

        // Assert
        Assert.That(group, Is.Not.Null);

        Assert.That(
            group!.Name,
            Is.EqualTo("АБЗ и ДСК МФ"));

        Assert.That(
            group.Type,
            Is.EqualTo("group"));

        Assert.That(
            group.ObjectIds,
            Is.Not.Null);
    }
}