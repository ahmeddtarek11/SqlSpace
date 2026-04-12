using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Auth;

/// <summary>
/// Manages refresh-token lifecycle with a one-token-per-user policy.
/// </summary>
/// <remarks>
/// Usage:
/// - Used by authentication workflows to maintain long-lived sessions securely.
///
/// When:
/// - Issue on login, rotate on refresh, revoke on logout or security event.
///
/// Why:
/// - Enforces rotation and revocation policy independent from controller logic.
/// - Provides secure long-lived session management.
/// - One active token per user (automatic logout from other devices).
///
/// Where:
/// - Interface consumed by auth orchestration services.
/// - Implemented in Infrastructure persistence/security.
///
/// How:
/// - Generate cryptographically strong token values using RandomNumberGenerator.
/// - Persist with expiry metadata.
/// - Hash tokens before storage (SHA256).
/// - One active token per user (replace on new login).
/// - Delete old tokens on rotation/revocation instead of storing revoked records.
/// </remarks>
public interface IRefreshTokenProvider
{
    /// <summary>
    /// Issues a new refresh token for a user, replacing existing active token if needed.
    /// </summary>
    /// <param name="userId">User identifier owning the token.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>Success payload with the newly persisted refresh token or failure errors.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate user identifier is not null/empty.
    /// 2. Generate secure random token:
    ///    - Use RandomNumberGenerator.GetBytes(32)
    ///    - Convert to Base64 string
    /// 3. Hash token for storage (SHA256).
    /// 4. Compute token expiry (UtcNow + 7 days from configuration).
    /// 5. Delete existing token for user if one exists (one-token-per-user policy).
    /// 6. Create new RefreshToken entity:
    ///    - Token (hashed)
    ///    - UserId
    ///    - ExpiresOnUtc
    ///    - CreatedOnUtc
    /// 7. Persist new token record.
    /// 8. Return created token (with original unhashed token for response).
    /// </remarks>
    Task<Result<RefreshToken>> IssueAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates an existing refresh token and returns the new active token.
    /// </summary>
    /// <param name="refreshToken">Current refresh token provided by client (unhashed).</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>Success payload with rotated token or failure errors.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Hash incoming token for database lookup (SHA256).
    /// 2. Query RefreshToken by hashed token value.
    /// 3. If not found, return failure (invalid token).
    /// 4. Check if token is expired (ExpiresOnUtc &lt; UtcNow) - return failure.
    /// 5. Delete old token record.
    /// 6. Generate new refresh token for same user (call IssueAsync).
    /// 7. Persist atomically.
    /// 8. Return new token entity.
    /// </remarks>
    Task<Result<RefreshToken>> RotateAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes the current active refresh token for a user by deleting it.
    /// </summary>
    /// <param name="userId">User identifier whose session should be invalidated.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>Success result when revocation is complete or failure errors.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query active refresh token by user id.
    /// 2. If found, delete token record.
    /// 3. Persist change to database.
    /// 4. Complete operation (no error if no active token found).
    /// </remarks>
    Task<Result> RevokeAsync(string userId, CancellationToken cancellationToken);
}
