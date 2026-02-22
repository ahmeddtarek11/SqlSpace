using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Infrastructure.Identity;

public sealed class AuthProvider : IAuthProvider
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthProvider> _logger;
    private readonly IJwtTokenProvider _tokenProvider;
    private readonly IRefreshTokenProvider _refreshTokenProvider;

    public AuthProvider(
        UserManager<ApplicationUser> userManager,
        ILogger<AuthProvider> logger,
        IJwtTokenProvider tokenProvider,
        IRefreshTokenProvider refreshTokenProvider)
    {
        _userManager = userManager;
        _logger = logger;
        _tokenProvider = tokenProvider;
        _refreshTokenProvider = refreshTokenProvider;
    }

    public async Task<Result<AuthTokensResult>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<AuthTokensResult>.Failure(AuthErrors.ValidationFailed("Request is required.", nameof(request)));
        }

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return Result<AuthTokensResult>.Failure(AuthErrors.InvalidCredentials(nameof(request)));
        }

        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null)
        {
            _logger.LogWarning("Login failed because user was not found. Email: {Email}", request.Email);
            return Result<AuthTokensResult>.Failure(AuthErrors.InvalidCredentials(nameof(request.Email)));
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            _logger.LogWarning("Login failed due to invalid password. Email: {Email}", request.Email);
            return Result<AuthTokensResult>.Failure(AuthErrors.InvalidCredentials(nameof(request.Password)));
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var accessTokenResult = await _tokenProvider.GenerateAccessTokenAsync(user.Id, user.Email!, user.UserName!, userRoles);
        if (accessTokenResult.IsFailure)
        {
            return Result<AuthTokensResult>.Failure(accessTokenResult.Errors);
        }

        var refreshTokenResult = await _refreshTokenProvider.IssueAsync(user.Id, cancellationToken);
        if (refreshTokenResult.IsFailure)
        {
            return Result<AuthTokensResult>.Failure(refreshTokenResult.Errors);
        }

        var refreshToken = refreshTokenResult.Value!;
        return Result<AuthTokensResult>.Success(new AuthTokensResult
        {
            AccessToken = accessTokenResult.Value!,
            RefreshToken = refreshToken.Token,
            ExpiresAt = refreshToken.ExpiresOnUtc,
            userId = user.Id
            
        });
    }

    public async Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Failure(AuthErrors.InvalidUserId(nameof(userId)));
        }

        return await _refreshTokenProvider.RevokeAsync(userId, cancellationToken);
    }

    public async Task<Result<AuthTokensResult>> RefreshAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<AuthTokensResult>.Failure(AuthErrors.InvalidRefreshToken(nameof(refreshToken)));
        }

        var rotatedTokenResult = await _refreshTokenProvider.RotateAsync(refreshToken, cancellationToken);
        if (rotatedTokenResult.IsFailure)
        {
            return Result<AuthTokensResult>.Failure(rotatedTokenResult.Errors);
        }

        var rotatedToken = rotatedTokenResult.Value!;
        var user = await _userManager.FindByIdAsync(rotatedToken.UserId);
        if (user is null)
        {
            _logger.LogWarning("Refresh failed because user was not found. UserId: {UserId}", rotatedToken.UserId);
            return Result<AuthTokensResult>.Failure(AuthErrors.UserNotFound(nameof(rotatedToken.UserId)));
        }

        var userRoles = await _userManager.GetRolesAsync(user);
        var accessTokenResult = await _tokenProvider.GenerateAccessTokenAsync(user.Id, user.Email!, user.UserName!, userRoles);
        if (accessTokenResult.IsFailure)
        {
            return Result<AuthTokensResult>.Failure(accessTokenResult.Errors);
        }

        return Result<AuthTokensResult>.Success(new AuthTokensResult
        {
            AccessToken = accessTokenResult.Value!,
            RefreshToken = rotatedToken.Token,
            ExpiresAt = rotatedToken.ExpiresOnUtc,
            userId = user.Id
        });
    }

    public async Task<Result<RegisterResult>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        if (request is null)
        {
            return Result<RegisterResult>.Failure(AuthErrors.ValidationFailed("Request is required.", nameof(request)));
        }

        var validationErrors = new List<Error>();
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            validationErrors.Add(AuthErrors.ValidationFailed("Email is required.", nameof(request.Email)));
        }

        if (string.IsNullOrWhiteSpace(request.Username))
        {
            validationErrors.Add(AuthErrors.ValidationFailed("Username is required.", nameof(request.Username)));
        }

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            validationErrors.Add(AuthErrors.ValidationFailed("Password is required.", nameof(request.Password)));
        }

        if (validationErrors.Count > 0)
        {
            return Result<RegisterResult>.Failure(validationErrors);
        }

        var user = new ApplicationUser
        {
            Email = request.Email,
            UserName = request.Username
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            _logger.LogInformation("Registration failed for email {Email}.", request.Email);
            return Result<RegisterResult>.Failure(MapIdentityErrors(identityResult));
        }

        return Result<RegisterResult>.Success(new RegisterResult
        {
            UserId = user.Id
        });
    }

    private static IReadOnlyList<Error> MapIdentityErrors(IdentityResult identityResult)
    {
        var errors = identityResult.Errors
            .Select(error => AuthErrors.ValidationFailed(error.Description, error.Code))
            .ToList();

        if (errors.Count == 0)
        {
            errors.Add(AuthErrors.Unexpected("Identity operation failed without details.", "identity"));
        }

        return errors;
    }
}
