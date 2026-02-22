using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Services.Auth;


///  application-layer wrapper around auth provider contracts.

public sealed class AuthService(IAuthProvider authProvider) : IAuthService
{
    private readonly IAuthProvider _authProvider = authProvider;

    public Task<Result<AuthTokensResult>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        return _authProvider.LoginAsync(request, cancellationToken);
    }

    public Task<Result> LogoutAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        return _authProvider.LogoutAsync(userId, cancellationToken);
    }

    public Task<Result<AuthTokensResult>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        return _authProvider.RefreshAsync(refreshToken, cancellationToken);
    }

    public Task<Result<RegisterResult>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        return _authProvider.RegisterAsync(request, cancellationToken);
    }
}
