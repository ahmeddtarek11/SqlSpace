namespace SqlSpace.Application.Abstractions.Auth;

/// <summary>
/// Authentication tokens response.
/// </summary>
public class AuthTokensResult
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string? userId { get; set; }
}
