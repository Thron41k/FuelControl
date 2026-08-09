using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using NUnit.Framework;

namespace FuelControl.Omnicomm.Tests.Authentication;

[TestFixture]
public sealed class OmnicommAuthenticationTests
{
    private HttpClient _httpClient = null!;

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
            BaseAddress = new Uri(_baseUrl)
        };

        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
    }

    [Test]
    public async Task Login_ShouldReturnJwtToken()
    {
        // Arrange
        var requestBody = new
        {
            login = _login,
            password = _password
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        // Act
        using var response = await _httpClient.PostAsync(
            "/auth/login?jwt=1",
            content);

        var responseBody =
            await response.Content.ReadAsStringAsync();

        // Выводим HTTP-результат.
        TestContext.WriteLine(
            $"HTTP Status: {(int)response.StatusCode} {response.StatusCode}");

        TestContext.WriteLine(
            $"Response Content-Type: {response.Content.Headers.ContentType}");

        TestContext.WriteLine(
            $"Response Length: {responseBody.Length}");

        // Не выводим сам responseBody,
        // поскольку в нём могут находиться токены.

        // Assert
        Assert.That(
            response.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            $"Omnicomm вернул ошибку: {SanitizeResponse(responseBody)}");

        Assert.That(
            responseBody,
            Is.Not.Null.And.Not.Empty,
            "Omnicomm вернул пустой ответ.");

        using var json = JsonDocument.Parse(responseBody);

        TestContext.WriteLine(
            $"JSON Root Type: {json.RootElement.ValueKind}");

        Assert.That(
            json.RootElement.ValueKind,
            Is.EqualTo(JsonValueKind.Object),
            "Ответ авторизации должен быть JSON-объектом.");

        var properties = json.RootElement
            .EnumerateObject()
            .Select(x => x.Name)
            .ToArray();

        TestContext.WriteLine(
            $"Response Properties: {string.Join(", ", properties)}");

        Assert.That(
            properties.Length,
            Is.GreaterThan(0),
            "Ответ не содержит JSON-свойств.");
    }

    [Test]
    public async Task Login_ShouldReturnTokenThatCanBeUsedForAuthorizedRequest()
    {
        // Arrange
        var requestBody = new
        {
            login = _login,
            password = _password
        };

        using var content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json");

        // Act — авторизация
        using var loginResponse = await _httpClient.PostAsync(
            "/auth/login?jwt=1",
            content);

        var loginResponseBody =
            await loginResponse.Content.ReadAsStringAsync();

        Assert.That(
            loginResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            $"Ошибка авторизации: {SanitizeResponse(loginResponseBody)}");

        var token = ExtractJwt(loginResponseBody);

        Assert.That(
            token,
            Is.Not.Null.And.Not.Empty,
            "JWT не найден в ответе Omnicomm.");

        TestContext.WriteLine("JWT получен успешно.");
        TestContext.WriteLine($"JWT Length: {token.Length}");

        // Проверяем, что JWT имеет стандартную структуру.
        var jwtParts = token.Split('.');

        Assert.That(
            jwtParts.Length,
            Is.EqualTo(3),
            "Полученное значение не похоже на JWT.");

        // Act — выполняем авторизованный запрос.
        //
        // Пока используем endpoint, который можно будет заменить
        // на конкретный метод API после проверки версии API.
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            "/api/service/geozones/geozones");

        request.Headers.Authorization =
            new AuthenticationHeaderValue("JWT", token);

        using var authorizedResponse =
            await _httpClient.SendAsync(request);

        var authorizedBody =
            await authorizedResponse.Content.ReadAsStringAsync();

        TestContext.WriteLine(
            $"Authorized request HTTP Status: " +
            $"{(int)authorizedResponse.StatusCode} " +
            $"{authorizedResponse.StatusCode}");

        TestContext.WriteLine(
            $"Authorized response length: {authorizedBody.Length}");

        // Мы проверяем именно отсутствие 401.
        //
        // Endpoint может вернуть другой код, например,
        // если у пользователя нет прав на конкретный ресурс.
        Assert.That(
            authorizedResponse.StatusCode,
            Is.Not.EqualTo(HttpStatusCode.Unauthorized),
            $"JWT был отклонён Omnicomm: {SanitizeResponse(authorizedBody)}");
    }

    private static string GetRequiredEnvironmentVariable(string name)
    {
        var value = Environment.GetEnvironmentVariable(name);

        if (string.IsNullOrWhiteSpace(value))
        {
            Assert.Fail(
                $"Не задана переменная окружения '{name}'.");
        }

        return value!;
    }

    private static string ExtractJwt(string json)
    {
        using var document = JsonDocument.Parse(json);

        if (!document.RootElement.TryGetProperty("jwt", out var jwtProperty))
            return string.Empty;

        return jwtProperty.GetString() ?? string.Empty;
    }

    private static string SanitizeResponse(string response)
    {
        if (string.IsNullOrWhiteSpace(response))
            return "<empty>";

        // Не показываем потенциальные токены в сообщении NUnit.
        return $"<response length: {response.Length}>";
    }

    [Test]
    public async Task Refresh_ShouldReturnNewJwt()
    {
        // Arrange
        var loginRequest = new
        {
            login = _login,
            password = _password
        };

        using var loginContent = new StringContent(
            JsonSerializer.Serialize(loginRequest),
            Encoding.UTF8,
            "application/json");

        using var loginResponse = await _httpClient.PostAsync(
            "/auth/login?jwt=1",
            loginContent);

        var loginBody =
            await loginResponse.Content.ReadAsStringAsync();

        Assert.That(
            loginResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            $"Ошибка авторизации. HTTP {(int)loginResponse.StatusCode}");

        using var loginJson = JsonDocument.Parse(loginBody);

        var refreshToken =
            loginJson.RootElement
                .GetProperty("refresh")
                .GetString();

        Assert.That(
            refreshToken,
            Is.Not.Null.And.Not.Empty,
            "Omnicomm не вернул refresh token.");

        TestContext.WriteLine(
            $"Refresh token получен. Length: {refreshToken.Length}");

        // Act
        using var refreshRequest = new HttpRequestMessage(
            HttpMethod.Post,
            "/auth/refresh");

        refreshRequest.Headers.TryAddWithoutValidation(
            "Authorization",
            $"JWT {refreshToken}");

        using var refreshResponse =
            await _httpClient.SendAsync(refreshRequest);

        var refreshBody =
            await refreshResponse.Content.ReadAsStringAsync();

        TestContext.WriteLine(
            $"Refresh HTTP Status: " +
            $"{(int)refreshResponse.StatusCode} " +
            $"{refreshResponse.StatusCode}");

        TestContext.WriteLine(
            $"Refresh response length: {refreshBody.Length}");

        // Assert
        Assert.That(
            refreshResponse.StatusCode,
            Is.EqualTo(HttpStatusCode.OK),
            "Omnicomm не принял refresh token.");

        using var refreshJson =
            JsonDocument.Parse(refreshBody);

        Assert.That(
            refreshJson.RootElement.TryGetProperty(
                "jwt",
                out var newJwtProperty),
            Is.True,
            "В ответе refresh отсутствует поле 'jwt'.");

        var newJwt = newJwtProperty.GetString();

        Assert.That(
            newJwt,
            Is.Not.Null.And.Not.Empty,
            "Omnicomm вернул пустой JWT.");

        TestContext.WriteLine(
            $"New JWT получен. Length: {newJwt.Length}");
    }
}