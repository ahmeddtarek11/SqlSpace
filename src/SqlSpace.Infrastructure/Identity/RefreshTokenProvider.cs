using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Identity;

public sealed class RefreshTokenProvider(
    IApplicationDbContext dbContext,
    ILogger<RefreshTokenProvider> logger,
    UserManager<ApplicationUser> userManager) : IRefreshTokenProvider
{
    private readonly IApplicationDbContext _dbContext = dbContext;
    private readonly UserManager<ApplicationUser> _userManager = userManager;
    private readonly ILogger<RefreshTokenProvider> _logger = logger;

    public async Task<Result<RefreshToken>> IssueAsync(string userId, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Issuing refresh token for userId: {UserId}", userId);

        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result<RefreshToken>.Failure(AuthErrors.InvalidUserId(nameof(userId)));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Issuing refresh token failed because user was not found. userId: {UserId}", userId);
            return Result<RefreshToken>.Failure(AuthErrors.UserNotFound(nameof(userId)));
        }

        var existingTokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId)
            .ToListAsync(cancellationToken);
        if (existingTokens.Count > 0)
        {
            _dbContext.RefreshTokens.RemoveRange(existingTokens);
        }

        var refreshTokenBytes = RandomNumberGenerator.GetBytes(64);
        var tokenString = Convert.ToBase64String(refreshTokenBytes);
        var hashedRefreshToken = HashToken(tokenString);

        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = hashedRefreshToken,
            UserId = user.Id,
            ExpiresOnUtc = DateTimeOffset.UtcNow.AddDays(7),
            CreatedOnUtc = DateTimeOffset.UtcNow
        };

        await _dbContext.RefreshTokens.AddAsync(refreshToken, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Refresh token issued successfully for userId: {UserId}", userId);

        // Return only the un-hashed token to the caller while keeping a hash in storage.
        refreshToken.Token = tokenString;
        return Result<RefreshToken>.Success(refreshToken);
    }

    public async Task<Result> RevokeAsync(string userId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Result.Failure(AuthErrors.InvalidUserId(nameof(userId)));
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user is null)
        {
            _logger.LogWarning("Refresh token revocation failed because user was not found. userId: {UserId}", userId);
            return Result.Failure(AuthErrors.UserNotFound(nameof(userId)));
        }

        var refreshTokens = await _dbContext.RefreshTokens
            .Where(token => token.UserId == userId)
            .ToListAsync(cancellationToken);

        if (refreshTokens.Count == 0)
        {
            return Result.Success();
        }

        _dbContext.RefreshTokens.RemoveRange(refreshTokens);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Refresh tokens revoked for userId: {UserId}", userId);

        return Result.Success();
    }

    public async Task<Result<RefreshToken>> RotateAsync(string refreshToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return Result<RefreshToken>.Failure(AuthErrors.InvalidRefreshToken(nameof(refreshToken)));
        }

        var hashedToken = HashToken(refreshToken);
        var existingToken = await _dbContext.RefreshTokens
            .FirstOrDefaultAsync(token => token.Token == hashedToken, cancellationToken);

        if (existingToken is null)
        {
            return Result<RefreshToken>.Failure(AuthErrors.RefreshTokenInvalid(nameof(refreshToken)));
        }

        if (existingToken.ExpiresOnUtc <= DateTimeOffset.UtcNow)
        {
            _logger.LogInformation("Found expired refresh token and deleting. TokenId: {TokenId}", existingToken.Id);
            _dbContext.RefreshTokens.Remove(existingToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result<RefreshToken>.Failure(AuthErrors.RefreshTokenExpired(nameof(refreshToken)));
        }

        return await IssueAsync(existingToken.UserId, cancellationToken);
    }

    private static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var tokenBytes = Encoding.UTF8.GetBytes(token);
        var hashedBytes = sha256.ComputeHash(tokenBytes);
        return Convert.ToBase64String(hashedBytes);
    }
}
