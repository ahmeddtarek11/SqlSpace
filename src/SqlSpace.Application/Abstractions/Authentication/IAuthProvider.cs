using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.Auth;

/// <summary>
/// Orchestrates authentication workflows including registration, login, token refresh, and logout.
/// </summary>
/// <remarks>
/// Usage:
/// - Used by application authentication services.
/// - Coordinates identity-provider operations, JWT generation, and refresh-token lifecycle.
///
/// When:
/// - User registers new account.
/// - User logs in with credentials.
/// - User refreshes access token.
/// - User logs out.
///
/// Why:
/// - Centralizes authentication business logic.
/// - Keeps controller logic thin.
/// - Enforces consistent token policies.
///
/// Where:
/// - Defined in Application as an abstraction (port).
/// - Consumed by application use-case services.
/// - Implemented in Infrastructure (for example, an ASP.NET Identity-based service).
///
/// How:
/// - Concrete implementation handles user management via an external auth provider/store.
/// - Uses JWT provider for access tokens.
/// - Uses refresh-token provider for long-lived sessions.
/// </remarks>
public interface IAuthProvider
{
    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">Registration request payload.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>Success payload with user ID or failure errors.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate email format and uniqueness.
    /// 2. Validate username uniqueness.
    /// 3. Validate password strength requirements.
    /// 4. Create ApplicationUser entity.
    /// 5. Hash password using the configured auth provider (automatic in most providers).
    /// 6. Persist user via the configured user store.
    /// 7. (Optional) Send email confirmation if enabled.
    /// 8. Return result with user ID or validation errors.
    /// </remarks>
    Task<Result<RegisterResult>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Authenticates a user by credentials and returns access/refresh token pair.
    /// </summary>
    /// <param name="request">Login request payload.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>Access and refresh tokens with expiration timestamps.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate email/password input format.
    /// 2. Find user by email via the configured user store.
    /// 3. Verify password using the configured auth provider.
    /// 4. Resolve user roles/claims.
    /// 5. Generate JWT access token via IJwtTokenProvider (15 minutes expiry).
    /// 6. Issue or replace refresh token via IRefreshTokenProvider (7 days expiry).
    /// 7. Return token payload for API response.
    /// </remarks>
    Task<Result<AuthTokensResult>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Exchanges a valid refresh token for a new access/refresh token pair.
    /// </summary>
    /// <param name="refreshToken">The existing refresh token presented by client.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>Success payload with new token pair or failure errors.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate incoming refresh token format.
    /// 2. Load token from storage via IRefreshTokenProvider.
    /// 3. Verify token exists and is not expired.
    /// 4. Resolve user context by UserId from token.
    /// 5. Resolve user roles/claims.
    /// 6. Generate new JWT access token via IJwtTokenProvider.
    /// 7. Rotate refresh token via IRefreshTokenProvider (delete old, create new).
    /// 8. Return updated token payload.
    /// </remarks>
    Task<Result<AuthTokensResult>> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken);

    /// <summary>
    /// Ends a user's active session by revoking refresh token state.
    /// </summary>
    /// <param name="userId">Authenticated user identifier from JWT claims.</param>
    /// <param name="cancellationToken">Cancellation token for request lifetime control.</param>
    /// <returns>Success result when logout side effects are persisted or failure errors.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate caller user identifier.
    /// 2. Locate active refresh token for the user via IRefreshTokenProvider.
    /// 3. Revoke token by deleting refresh token record.
    /// 4. Persist changes to database.
    /// 5. Complete request (client should discard access token).
    /// </remarks>
    Task<Result> LogoutAsync(string userId, CancellationToken cancellationToken);
}
