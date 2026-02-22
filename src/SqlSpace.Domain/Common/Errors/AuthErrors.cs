
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Domain.Common.Errors;

public static class AuthErrors
{
    public const string InvalidCredentialsCode = "auth.invalid_credentials";
    public const string UserNotFoundCode = "auth.user_not_found";
    public const string RefreshTokenInvalidCode = "auth.refresh_token_invalid";
    public const string RefreshTokenExpiredCode = "auth.refresh_token_expired";
    public const string InvalidUserIdCode = "auth.invalid_user_id";
    public const string InvalidRefreshTokenCode = "auth.invalid_refresh_token";
    public const string ValidationFailedCode = "auth.validation_failed";
    public const string JwtConfigMissingCode = "auth.jwt_config_missing";
    public const string UnexpectedCode = "auth.unexpected";

    public static Error InvalidCredentials(string? target = null) =>
        new(InvalidCredentialsCode, "Invalid email or password.", target);

    public static Error UserNotFound(string? target = null) =>
        new(UserNotFoundCode, "User was not found.", target);

    public static Error RefreshTokenInvalid(string? target = null) =>
        new(RefreshTokenInvalidCode, "Refresh token is invalid.", target);

    public static Error RefreshTokenExpired(string? target = null) =>
        new(RefreshTokenExpiredCode, "Refresh token is expired.", target);

    public static Error InvalidUserId(string? target = null) =>
        new(InvalidUserIdCode, "User identifier is invalid.", target);

    public static Error InvalidRefreshToken(string? target = null) =>
        new(InvalidRefreshTokenCode, "Refresh token is required.", target);

    public static Error ValidationFailed(string message, string? target = null) =>
        new(ValidationFailedCode, message, target);

    public static Error JwtConfigMissing(string setting, string? target = null) =>
        new(JwtConfigMissingCode, $"JWT setting '{setting}' is missing or invalid.", target);

    public static Error Unexpected(string message, string? target = null) =>
        new(UnexpectedCode, message, target);
}
