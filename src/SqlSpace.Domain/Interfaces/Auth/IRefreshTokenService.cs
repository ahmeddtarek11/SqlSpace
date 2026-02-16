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
///
/// Where:
/// - Interface consumed by auth orchestration services.
/// - Implementation belongs to Infrastructure persistence/security.
///
/// How:
/// - Generate cryptographically strong token values.
/// - Persist with expiry and revocation metadata.
/// </remarks>
public interface IRefreshTokenService
{
    /// <summary>
    /// Issues a new refresh token for a user, replacing existing active token if needed.
    /// </summary>
    /// <param name="userId">User identifier owning the token.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>The newly persisted refresh-token entity.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate user identifier.
    /// 2. Generate secure random token.
    /// 3. Compute token expiry using configuration.
    /// 4. Revoke existing active token (if present).
    /// 5. Persist new token record.
    /// 6. Return created token.
    /// </remarks>
    Task<RefreshToken> IssueAsync(string userId, CancellationToken cancellationToken);

    /// <summary>
    /// Rotates an existing refresh token and returns the new active token.
    /// </summary>
    /// <param name="refreshToken">Current refresh token provided by client.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>New refresh token when rotation succeeds; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Locate incoming token in persistence.
    /// 2. Validate token is active and not expired/revoked.
    /// 3. Revoke old token.
    /// 4. Generate replacement token.
    /// 5. Persist replacement token atomically.
    /// 6. Return new token entity.
    /// </remarks>
    Task<RefreshToken?> RotateAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Revokes the current active refresh token for a user.
    /// </summary>
    /// <param name="userId">User identifier whose session should be invalidated.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>A task that completes after token revocation is persisted.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Locate active token by user id.
    /// 2. Mark token revoked or delete record.
    /// 3. Persist change.
    /// 4. Complete operation.
    /// </remarks>
    Task RevokeAsync(string userId, CancellationToken cancellationToken);
}
