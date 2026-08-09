using FuelControl.Omnicomm.Authentication;
using FuelControl.Omnicomm.Http;
using NUnit.Framework;

namespace FuelControl.Omnicomm.Tests.Http;

[TestFixture]
public sealed class OmnicommApiClientTests
{
    private HttpClient _httpClient = null!;
    private IOmnicommAuthenticator _authenticator = null!;
    private IOmnicommApiClient _client = null!;

    private string _baseUrl = null!;
    private string _login = null!;
    private string _password = null!;

    [SetUp]
    public void SetUp()
    {
        _baseUrl = "https://online.omnicomm.ru";
        _login = "";
        _password = "";

        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(_baseUrl),
            Timeout = TimeSpan.FromSeconds(30)
        };

        _authenticator =
            new OmnicommAuthenticator(_httpClient);

        var credentials =
            new OmnicommCredentials(
                _login,
                _password);

        _client =
            new OmnicommApiClient(
                _httpClient,
                _authenticator,
                credentials);
    }

    [TearDown]
    public void TearDown()
    {
        _client.Dispose();
        _httpClient.Dispose();
    }

    [Test]
    public async Task SendAsync_ShouldAuthenticateAutomatically()
    {
        // Act
        using var response = await _client.SendAsync(
            () => new HttpRequestMessage(
                HttpMethod.Get,
                "/"));

        // Assert
        TestContext.WriteLine(
            $"HTTP Status: {(int)response.StatusCode} " +
            $"{response.StatusCode}");

        Assert.That(
            response.StatusCode,
            Is.Not.EqualTo(
                System.Net.HttpStatusCode.Unauthorized));
    }

    private static string GetRequiredEnvironmentVariable(
        string name)
    {
        var value =
            Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
        {
            Assert.Fail(
                $"Не задана переменная окружения '{name}'.");
        }

        return value!;
    }
}