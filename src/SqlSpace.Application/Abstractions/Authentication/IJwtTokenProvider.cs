
using SqlSpace.Domain.Common.Results;

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
/// - Implemented in Infrastructure security layer.
///
/// How:
/// - Build claims principal data from user id/email/roles.
/// - Create signed JWT using configured options (SymmetricSecurityKey, SigningCredentials).
/// - Use System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler.
/// </remarks>
public interface IJwtTokenProvider
{
    /// <summary>
    /// Generates a signed JWT access token for a user session.
    /// </summary>
    /// <param name="userId">User identifier placed in token subject claim.</param>
    /// <param name="email">User email claim used by downstream APIs.</param>
    /// <param name="username">User display name for UI presentation.</param>
    /// <param name="roles">User role collection encoded in role claims.</param>
    /// <returns>Success payload with serialized JWT string or failure errors.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate required identity data (userId, email not null/empty).
    /// 2. Create claims list:
    ///    - ClaimTypes.NameIdentifier (sub): userId
    ///    - ClaimTypes.Email: email
    ///    - ClaimTypes.Name: username
    ///    - ClaimTypes.Role: each role (multiple claims if multiple roles)
    ///    - JwtRegisteredClaimNames.Jti: new GUID (token ID)
    /// 3. Load JWT settings from configuration:
    ///    - Secret key (min 256 bits)
    ///    - Issuer
    ///    - Audience
    ///    - Expiry (default: 15 minutes)
    /// 4. Create SymmetricSecurityKey from secret.
    /// 5. Create SigningCredentials with HmacSha256.
    /// 6. Build JwtSecurityToken with:
    ///    - issuer, audience, claims, notBefore, expires, signingCredentials
    /// 7. Write token using JwtSecurityTokenHandler.
    /// 8. Return compact serialized token string.
    /// </remarks>
    Task<Result<string>> GenerateAccessTokenAsync(
        string userId,
        string email,
        string username,
        ICollection<string> roles
        );
}
