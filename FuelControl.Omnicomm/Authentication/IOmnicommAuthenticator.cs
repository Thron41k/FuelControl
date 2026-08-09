namespace FuelControl.Omnicomm.Authentication;

public interface IOmnicommAuthenticator
{
    Task<OmnicommTokens> LoginAsync(
        OmnicommCredentials credentials,
        CancellationToken cancellationToken = default);

    Task<OmnicommTokens> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}