namespace FuelControl.Omnicomm.Exceptions;

public sealed class OmnicommApiException : Exception
{
    public int StatusCode { get; }

    public string? ResponseBody { get; }

    public OmnicommApiException(
        int statusCode,
        string? responseBody = null)
        : base($"Omnicomm API вернул HTTP {statusCode}.")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}