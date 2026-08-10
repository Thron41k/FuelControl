using FuelControl.Omnicomm.Http;
using FuelControl.Omnicomm.Vehicles;
using FuelControl.Omnicomm.Vehicles.Models;
using System.Text.Json;
using System.Text.Json.Serialization;
using OmnicommWantedList =
    FuelControl.Omnicomm.Vehicles.Models.OmnicommWantedListResponse;
namespace FuelControl.Omnicomm.Tests.Vehicles;

[TestFixture]
public sealed class OmnicommVehicleClientTests
{
    [Test]
    public async Task GetWantedObjectsAsync_ShouldSendCorrectParametersAndReturnObjects()
    {
        // Arrange
        const long actorId = 1005704; 
        long[] groupIds = [ 5128, 5246, 5129 ]; 
        long[] objectIds = [ 217002648, 236040081, 203029941 ]; 
        var expectedObjects = new[]
        {
            new OmnicommObject { Id = 217002648, Name = "Горелка RL50 (marini) (006) WF", Type = "vehicle", SystemType = "FAS", Value = 0, Flag = false }, new OmnicommObject { Id = 236040081, Name = "ДСК DBY-ELS-0801-150 Т/Н (12)", Type = "vehicle", SystemType = "FAS", Value = 0, Flag = false }, new OmnicommObject { Id = 203029941, Name = "ДСК DBY-ELS-0801-150 Т/Н (14)", Type = "vehicle", SystemType = "FAS", Value = 0, Flag = false }
        }; 
        var expectedResponse = new OmnicommWantedListResponse
        {
            Objects = expectedObjects
        }; 
        var fakeApiClient = new CapturingOmnicommApiClient( expectedResponse); 
        var client = new OmnicommVehicleClient(fakeApiClient); 
        // Act
        var result = await client.GetWantedObjectsAsync( groupIds, objectIds, actorId);
        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That( result, Has.Count.EqualTo(3)); 
        Assert.That( result.Select(x => x.Id), Is.EqualTo(objectIds)); 
        Assert.That( result[0].Name, Is.EqualTo("Горелка RL50 (marini) (006) WF")); 
        Assert.That( result[1].Name, Is.EqualTo("ДСК DBY-ELS-0801-150 Т/Н (12)")); 
        Assert.That( result[2].Name, Is.EqualTo("ДСК DBY-ELS-0801-150 Т/Н (14)")); 
        // Проверяем сам запрос к Omnicomm.
        Assert.That( fakeApiClient.LastRequestUri, Is.EqualTo("/vehiclesTree"));
        Assert.That( fakeApiClient.LastParameters, Is.Not.Null); 
        var parameters = fakeApiClient.LastParameters!;
        Assert.That( parameters["action"], Is.EqualTo("getWantedList")); 
        Assert.That( parameters["allInclusive"], Is.EqualTo("true")); 
        Assert.That( parameters["actorID"], Is.EqualTo(actorId.ToString())); 
        // Проверяем wanted.
        var wanted =
            JsonSerializer.Deserialize<OmnicommWanted>(
                parameters["wanted"]);
        Assert.That(wanted, Is.Not.Null); 
        Assert.That( wanted!.Groups, Is.EqualTo(groupIds)); 
        Assert.That( wanted.Objects, Is.EqualTo(objectIds)); 
        // Проверяем alreadyHave.
        var alreadyHave = JsonSerializer.Deserialize< WantedParameters>( parameters["alreadyHave"]); 
        Assert.That(alreadyHave, Is.Not.Null);
        Assert.That( alreadyHave!.Groups, Is.Empty); 
        Assert.That( alreadyHave.Objects, Is.Empty);
    }
    public sealed class OmnicommWanted
    {
        [JsonPropertyName("groups")]
        public IReadOnlyList<long> Groups { get; init; } = [];
        [JsonPropertyName("objects")]
        public IReadOnlyList<long> Objects { get; init; } = [];
    }
    private sealed class CapturingOmnicommApiClient
        : IOmnicommApiClient
    {
        private readonly OmnicommWantedList _response;

        public string? LastRequestUri { get; private set; }

        public IReadOnlyDictionary<string, string>?
            LastParameters
        { get; private set; }

        public CapturingOmnicommApiClient(
            OmnicommWantedList response)
        {
            _response = response;
        }

        public Task<TResponse?> PostFormAsync<TResponse>(
            string requestUri,
            IReadOnlyDictionary<string, string> parameters,
            CancellationToken cancellationToken = default)
        {
            LastRequestUri = requestUri;
            LastParameters = parameters;

            if (typeof(TResponse) != typeof(OmnicommWantedList))
            {
                throw new InvalidOperationException(
                    $"Неожиданный тип ответа: {typeof(TResponse).FullName}");
            }

            return Task.FromResult(
                (TResponse?)(object)_response);
        }

        public Task<HttpResponseMessage> SendAsync(
            Func<HttpRequestMessage> requestFactory,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<T?> GetAsync<T>(
            string requestUri,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<TResponse?> PostAsync<TRequest, TResponse>(
            string requestUri,
            TRequest request,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public void Dispose()
        {
        }
    }

    private sealed class WantedParameters
    {
        public long[] Groups { get; set; } = [];
        public long[] Objects { get; set; } = [];
    }

}