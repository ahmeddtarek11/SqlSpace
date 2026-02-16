namespace SqlSpace.Application.Abstractions.Auth;

/// <summary>
/// Creates JWT access tokens for authenticated users.
/// </summary>
/// <remarks>
/// Usage:
/// - Called by auth session orchestration after user identity is verified.
///
/// When:
/// - During login and refresh flows.
///
/// Why:
/// - Centralizes JWT construction (claims, signing key, expiry, issuer/audience rules).
///
/// Where:
/// - Interface consumed by auth application services.
/// - Implementation belongs to Infrastructure security layer.
///
/// How:
/// - Build claims principal data from user id/email/roles.
/// - Create signed JWT using configured options.
/// </remarks>
public interface IJwtTokenService
{
    /// <summary>
    /// Generates a signed JWT access token for a user session.
    /// </summary>
    /// <param name="userId">User identifier placed in token subject claim.</param>
    /// <param name="email">User email claim used by downstream APIs.</param>
    /// <param name="roles">User role collection encoded in role claims.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>Serialized JWT access token string.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate required identity data.
    /// 2. Create standard and custom claims.
    /// 3. Load JWT signing/encryption settings.
    /// 4. Build security token descriptor with expiry.
    /// 5. Sign and serialize token.
    /// 6. Return final compact token string.
    /// </remarks>
    Task<string> GenerateAccessTokenAsync(
        string userId,
        string email,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken);
}
