using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FuelControl.Omnicomm.Authentication;
using FuelControl.Omnicomm.Exceptions;
using FuelControl.Omnicomm.Vehicles.Serialization;

namespace FuelControl.Omnicomm.Http;

public sealed class OmnicommApiClient(
    HttpClient httpClient,
    IOmnicommAuthenticator authenticator,
    OmnicommCredentials credentials)
    : IOmnicommApiClient
{
    private readonly SemaphoreSlim _authenticationLock = new(1, 1);

    private OmnicommTokens? _tokens;

    private static readonly JsonSerializerOptions JsonOptions =
        CreateJsonOptions();

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        options.Converters.Add(
            new OmnicommVehicleGroupConverter());

        return options;
    }

    public async Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        await EnsureAuthenticatedAsync(cancellationToken);

        using var request = requestFactory();

        AddAuthorizationHeader(
            request,
            _tokens!.Jwt);

        var response = await httpClient.SendAsync(
            request,
            cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();

        await RefreshAuthenticationAsync(cancellationToken);

        using var retryRequest = requestFactory();

        AddAuthorizationHeader(
            retryRequest,
            _tokens!.Jwt);

        return await httpClient.SendAsync(
            retryRequest,
            cancellationToken);
    }

    public async Task<T?> GetAsync<T>(
        string requestUri,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () => new HttpRequestMessage(
                HttpMethod.Get,
                requestUri),
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<T>(
            JsonOptions,
            cancellationToken);
    }

    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default)
    {
        using var response = await SendAsync(
            () =>
            {
                var message = new HttpRequestMessage(
                    HttpMethod.Post,
                    requestUri);

                message.Content =
                    JsonContent.Create(
                        request,
                        options: JsonOptions);

                return message;
            },
            cancellationToken);

        await EnsureSuccessAsync(response, cancellationToken);

        return await response.Content.ReadFromJsonAsync<TResponse>(
            JsonOptions,
            cancellationToken);
    }

    private async Task EnsureAuthenticatedAsync(
        CancellationToken cancellationToken)
    {
        if (_tokens is not null &&
            !string.IsNullOrWhiteSpace(_tokens.Jwt))
        {
            return;
        }

        await _authenticationLock.WaitAsync(
            cancellationToken);

        try
        {
            if (_tokens is not null &&
                !string.IsNullOrWhiteSpace(_tokens.Jwt))
            {
                return;
            }

            _tokens = await authenticator.LoginAsync(
                credentials,
                cancellationToken);
        }
        finally
        {
            _authenticationLock.Release();
        }
    }

    private async Task RefreshAuthenticationAsync(
        CancellationToken cancellationToken)
    {
        await _authenticationLock.WaitAsync(
            cancellationToken);

        try
        {
            _tokens = await RefreshOrLoginAsync(
                cancellationToken);
        }
        finally
        {
            _authenticationLock.Release();
        }
    }

    private async Task<OmnicommTokens> RefreshOrLoginAsync(
        CancellationToken cancellationToken)
    {
        if (_tokens is not null &&
            !string.IsNullOrWhiteSpace(_tokens.Refresh))
        {
            try
            {
                return await authenticator.RefreshAsync(
                    _tokens.Refresh,
                    cancellationToken);
            }
            catch (HttpRequestException)
            {
                // Refresh token недействителен.
                // Выполняем полную авторизацию.
            }
        }

        return await authenticator.LoginAsync(
            credentials,
            cancellationToken);
    }

    private static void AddAuthorizationHeader(
        HttpRequestMessage request,
        string jwt)
    {
        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "JWT",
                jwt);
    }

    private static async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var body =
            await response.Content.ReadAsStringAsync(
                cancellationToken);

        throw new OmnicommApiException(
            (int)response.StatusCode,
            body);
    }

    public void Dispose()
    {
        _authenticationLock.Dispose();
    }
}