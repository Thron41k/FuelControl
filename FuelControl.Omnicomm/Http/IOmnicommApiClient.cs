using FuelControl.Omnicomm.Reports.Models;

namespace FuelControl.Omnicomm.Http;

public interface IOmnicommApiClient : IDisposable
{
    Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default);

    Task<T?> GetAsync<T>(
        string requestUri,
        CancellationToken cancellationToken = default);

    Task<TResponse?> PostAsync<TRequest, TResponse>(
        string requestUri,
        TRequest request,
        CancellationToken cancellationToken = default);

    Task<TResponse?> PostFormAsync<TResponse>(
        string requestUri,
        IReadOnlyDictionary<string, string> parameters,
        CancellationToken cancellationToken = default);
}