// FuelControl.Omnicomm.Tests/Fakes/FakeOmnicommApiClient.cs
using System.Net;
using System.Text;
using FuelControl.Omnicomm.Http;

namespace FuelControl.Omnicomm.Tests.Fakes;

internal sealed class FakeOmnicommApiClient : IOmnicommApiClient
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _handler;

    public HttpRequestMessage? LastRequest { get; private set; }

    public string? LastRequestBody { get; private set; }

    public FakeOmnicommApiClient(
        Func<HttpRequestMessage, HttpResponseMessage> handler)
    {
        _handler = handler;
    }

    public Task<HttpResponseMessage> SendAsync(
        Func<HttpRequestMessage> requestFactory,
        CancellationToken cancellationToken = default)
    {
        var request = requestFactory();
        LastRequest = request;

        if (request.Content is not null)
        {
            LastRequestBody = request.Content
                .ReadAsStringAsync(cancellationToken)
                .GetAwaiter()
                .GetResult();
        }

        var response = _handler(request);
        return Task.FromResult(response);
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

    public Task<T?> PostFormAsync<T>(string requestUri, IEnumerable<KeyValuePair<string, string>> formData, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
    }

    public static HttpResponseMessage JsonResponse(
        string json,
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new HttpResponseMessage(statusCode)
        {
            Content = new StringContent(
                json,
                Encoding.UTF8,
                "application/json")
        };
    }
}