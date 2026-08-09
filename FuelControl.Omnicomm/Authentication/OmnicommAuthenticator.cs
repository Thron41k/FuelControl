using System.Net.Http.Json;
using System.Text.Json;

namespace FuelControl.Omnicomm.Authentication;

public sealed class OmnicommAuthenticator(
    HttpClient httpClient) : IOmnicommAuthenticator
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<OmnicommTokens> LoginAsync(
        OmnicommCredentials credentials,
        CancellationToken cancellationToken = default)
    {
        var request = new
        {
            login = credentials.Login,
            password = credentials.Password
        };

        using var response = await httpClient.PostAsJsonAsync(
            "/auth/login?jwt=1",
            request,
            JsonOptions,
            cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Ошибка авторизации Omnicomm. " +
                $"HTTP {(int)response.StatusCode}.");
        }

        var result =
            JsonSerializer.Deserialize<OmnicommTokens>(
                responseBody,
                JsonOptions);

        if (result is null ||
            string.IsNullOrWhiteSpace(result.Jwt) ||
            string.IsNullOrWhiteSpace(result.Refresh))
        {
            throw new InvalidOperationException(
                "Omnicomm вернул некорректный ответ авторизации.");
        }

        return result;
    }

    public async Task<OmnicommTokens> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "/auth/refresh");

        request.Headers.TryAddWithoutValidation(
            "Authorization",
            $"JWT {refreshToken}");

        using var response =
            await httpClient.SendAsync(
                request,
                cancellationToken);

        var responseBody =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"Ошибка обновления JWT Omnicomm. " +
                $"HTTP {(int)response.StatusCode}.");
        }

        var result =
            JsonSerializer.Deserialize<OmnicommTokens>(
                responseBody,
                JsonOptions);

        if (result is null ||
            string.IsNullOrWhiteSpace(result.Jwt) ||
            string.IsNullOrWhiteSpace(result.Refresh))
        {
            throw new InvalidOperationException(
                "Omnicomm вернул некорректный ответ refresh.");
        }

        return result;
    }
}