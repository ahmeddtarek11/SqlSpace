namespace SqlSpace.Application.Abstractions.Auth;

// Token payload returned to API clients.
public sealed class AuthTokensResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiresAtUtc { get; init; }
    public DateTime RefreshTokenExpiresAtUtc { get; init; }
}
