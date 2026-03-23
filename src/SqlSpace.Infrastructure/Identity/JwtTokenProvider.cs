using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Infrastructure.Identity;

public sealed class JwtTokenProvider : IJwtTokenProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<JwtTokenProvider> _logger;

    public JwtTokenProvider(IConfiguration configuration, ILogger<JwtTokenProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<Result<string>> GenerateAccessTokenAsync(
        string userId,
        string email,
        string username,
        ICollection<string> roles)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Task.FromResult(Result<string>.Failure(AuthErrors.InvalidUserId(nameof(userId))));
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            return Task.FromResult(Result<string>.Failure(AuthErrors.ValidationFailed("Email is required.", nameof(email))));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            return Task.FromResult(Result<string>.Failure(AuthErrors.ValidationFailed("Username is required.", nameof(username))));
        }

        _logger.LogDebug("Generating access token for userId: {UserId}", userId);

        var settings = _configuration.GetSection("JwtSettings");
        var audience = settings["Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            _logger.LogError("JWT config missing: Audience (JwtSettings:Audience)");
            return Task.FromResult(Result<string>.Failure(AuthErrors.JwtConfigMissing("Audience", "JwtSettings:Audience")));
        }

        var issuer = settings["Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            _logger.LogError("JWT config missing: Issuer (JwtSettings:Issuer)");
            return Task.FromResult(Result<string>.Failure(AuthErrors.JwtConfigMissing("Issuer", "JwtSettings:Issuer")));
        }

        var secretKey = settings["Secret"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            _logger.LogError("JWT config missing: Secret (JwtSettings:Secret)");
            return Task.FromResult(Result<string>.Failure(AuthErrors.JwtConfigMissing("Secret", "JwtSettings:Secret")));
        }

        if (!int.TryParse(settings["TokenExpirationInMinutes"], out var tokenExpirationMinutes) || tokenExpirationMinutes <= 0)
        {
            _logger.LogError("JWT config missing or invalid: TokenExpirationInMinutes (JwtSettings:TokenExpirationInMinutes)");
            return Task.FromResult(Result<string>.Failure(AuthErrors.JwtConfigMissing(
                "TokenExpirationInMinutes",
                "JwtSettings:TokenExpirationInMinutes")));
        }

        try
        {
            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, userId),
                new(JwtRegisteredClaimNames.Email, email),
                new(JwtRegisteredClaimNames.Name, username),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            foreach (var role in roles ?? Array.Empty<string>())
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var descriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(tokenExpirationMinutes),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var securityToken = tokenHandler.CreateToken(descriptor);
            var token = tokenHandler.WriteToken(securityToken);
            _logger.LogDebug("Access token generated successfully for userId: {UserId}, expiresInMinutes: {ExpirationMinutes}", userId, tokenExpirationMinutes);
            return Task.FromResult(Result<string>.Success(token));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate JWT access token for userId: {UserId}", userId);
            return Task.FromResult(Result<string>.Failure(AuthErrors.Unexpected("Failed to generate JWT access token.", "jwt")));
        }
    }
}
