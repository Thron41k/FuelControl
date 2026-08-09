namespace FuelControl.Omnicomm.Authentication;

public sealed record OmnicommTokens(
    string Jwt,
    string Refresh);