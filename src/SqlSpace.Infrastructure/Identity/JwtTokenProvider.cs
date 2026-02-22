using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Infrastructure.Identity;

public sealed class JwtTokenProvider : IJwtTokenProvider
{
    private readonly IConfiguration _configuration;

    public JwtTokenProvider(IConfiguration configuration)
    {
        _configuration = configuration;
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

        var settings = _configuration.GetSection("JwtSettings");
        var audience = settings["Audience"];
        if (string.IsNullOrWhiteSpace(audience))
        {
            return Task.FromResult(Result<string>.Failure(AuthErrors.JwtConfigMissing("Audience", "JwtSettings:Audience")));
        }

        var issuer = settings["Issuer"];
        if (string.IsNullOrWhiteSpace(issuer))
        {
            return Task.FromResult(Result<string>.Failure(AuthErrors.JwtConfigMissing("Issuer", "JwtSettings:Issuer")));
        }

        var secretKey = settings["Secret"];
        if (string.IsNullOrWhiteSpace(secretKey))
        {
            return Task.FromResult(Result<string>.Failure(AuthErrors.JwtConfigMissing("Secret", "JwtSettings:Secret")));
        }

        if (!int.TryParse(settings["TokenExpirationInMinutes"], out var tokenExpirationMinutes) || tokenExpirationMinutes <= 0)
        {
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
            return Task.FromResult(Result<string>.Success(token));
        }
        catch (Exception)
        {
            return Task.FromResult(Result<string>.Failure(AuthErrors.Unexpected("Failed to generate JWT access token.", "jwt")));
        }
    }
}
