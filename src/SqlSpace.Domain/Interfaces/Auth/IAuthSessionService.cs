namespace SqlSpace.Application.Abstractions.Auth;

/// <summary>
/// Orchestrates authentication session workflows exposed to API endpoints.
/// </summary>
/// <remarks>
/// Usage:
/// - Used by login/refresh/logout endpoints.
/// - Coordinates user validation, JWT generation, and refresh-token lifecycle.
///
/// When:
/// - At the auth boundary where credentials or refresh tokens are submitted.
///
/// Why:
/// - Keeps controller logic thin and centralizes session policy in one service.
///
/// Where:
/// - Interface consumed by API/Application.
/// - Implementation belongs to Infrastructure/Application service layer.
///
/// How:
/// - Delegates token creation to token services and persistence to refresh-token storage.
/// </remarks>
public interface IAuthSessionService
{
    /// <summary>
    /// Authenticates a user by credentials and returns a fresh access/refresh token pair.
    /// </summary>
    /// <param name="email">User login email.</param>
    /// <param name="password">User login password.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>Access and refresh tokens with their expiration timestamps.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate email/password input format.
    /// 2. Verify credentials against identity store.
    /// 3. Resolve user roles/claims.
    /// 4. Generate JWT access token.
    /// 5. Issue or replace refresh token.
    /// 6. Return token payload for API response.
    /// </remarks>
    Task<AuthTokensResult> LoginAsync(string email, string password, CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges a valid refresh token for a new access/refresh token pair.
    /// </summary>
    /// <param name="refreshToken">The existing refresh token presented by client.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>New token pair when rotation succeeds; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate incoming refresh token shape.
    /// 2. Load token from storage and verify expiry/revocation.
    /// 3. Resolve user context and roles.
    /// 4. Generate new JWT access token.
    /// 5. Rotate refresh token (invalidate old, persist new).
    /// 6. Return updated token payload.
    /// </remarks>
    Task<AuthTokensResult?> RefreshAsync(string refreshToken, CancellationToken cancellationToken);

    /// <summary>
    /// Ends a user's active session by revoking refresh token state.
    /// </summary>
    /// <param name="userId">Authenticated user identifier.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>A task that completes when logout side effects are persisted.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate caller user identifier.
    /// 2. Locate active refresh token for the user.
    /// 3. Revoke/remove token from persistence.
    /// 4. Persist changes and finish request.
    /// </remarks>
    Task LogoutAsync(string userId, CancellationToken cancellationToken);
}
