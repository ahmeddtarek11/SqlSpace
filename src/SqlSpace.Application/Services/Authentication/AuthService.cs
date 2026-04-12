using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Services.Auth;


///  application-layer wrapper around auth provider contracts.

public sealed class AuthService(IAuthProvider authProvider, ILogger<AuthService> logger) : IAuthService
{
    private readonly IAuthProvider _authProvider = authProvider;
    private readonly ILogger<AuthService> _logger = logger;

    public Task<Result<AuthTokensResult>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Login requested. Email: {Email}", request?.Email);
        return _authProvider.LoginAsync(request!, cancellationToken);
    }

    public Task<Result> LogoutAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Logout requested. UserId: {UserId}", userId);
        return _authProvider.LogoutAsync(userId, cancellationToken);
    }

    public Task<Result<AuthTokensResult>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken)
    {
        _logger.LogDebug("Token refresh requested.");
        return _authProvider.RefreshAsync(refreshToken, cancellationToken);
    }

    public Task<Result<RegisterResult>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Register requested. Email: {Email}, Username: {Username}", request?.Email, request?.Username);
        return _authProvider.RegisterAsync(request!, cancellationToken);
    }
}
