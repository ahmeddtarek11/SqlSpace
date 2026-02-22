
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.Auth;

/// <summary>
/// Application-facing authentication use cases consumed by API endpoints.
/// </summary>
public interface IAuthService
{
    Task<Result<RegisterResult>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    Task<Result<AuthTokensResult>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    Task<Result<AuthTokensResult>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    Task<Result> LogoutAsync(
        string userId,
        CancellationToken cancellationToken);
}
