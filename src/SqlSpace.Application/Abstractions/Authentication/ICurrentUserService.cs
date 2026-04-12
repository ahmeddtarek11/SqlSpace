namespace SqlSpace.Application.Abstractions.Auth;

/// <summary>
/// Provides access to current authenticated user information from JWT token claims.
/// </summary>
/// <remarks>
/// Usage:
/// - Used throughout application to identify the authenticated user making requests.
/// - Accessed by services to enforce user-specific business rules and authorization.
///
/// When:
/// - Any service needs to know who is making the request.
/// - Authorization checks require current user identity.
/// - Audit logging needs to capture acting user.
///
/// Why:
/// - Centralizes JWT claims parsing and user identity resolution.
/// - Provides consistent user access across all application services.
/// - Abstracts HttpContext access behind clean interface.
///
/// Where:
/// - Interface consumed by all application services and controllers.
/// - Implemented in API layer as a web request-context adapter using IHttpContextAccessor.
///
/// How:
/// - Access HttpContext.User claims principal.
/// - Extract standard JWT claims (sub, email, name, roles).
/// - Return null when user is not authenticated.
/// </remarks>
public interface ICurrentUserService
{
    /// <summary>
    /// Gets the unique identifier of the currently authenticated user.
    /// </summary>
    /// <returns>User identifier from JWT "sub" claim, or null if not authenticated.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Access HttpContext.User from IHttpContextAccessor.
    /// 2. Check if user is authenticated.
    /// 3. Extract "sub" (subject) claim from JWT.
    /// 4. Return claim value or null if not found/authenticated.
    /// </remarks>
    string? GetUserId();

    /// <summary>
    /// Gets the email address of the currently authenticated user.
    /// </summary>
    /// <returns>User email from JWT "email" claim, or null if not available.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Access HttpContext.User from IHttpContextAccessor.
    /// 2. Extract "email" claim from JWT.
    /// 3. Return claim value or null if not found.
    /// </remarks>
    string? GetUserEmail();

    /// <summary>
    /// Gets the username of the currently authenticated user.
    /// </summary>
    /// <returns>Username from JWT "name" claim, or null if not available.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Access HttpContext.User from IHttpContextAccessor.
    /// 2. Extract "name" claim from JWT.
    /// 3. Return claim value or null if not found.
    /// </remarks>
    string? GetUserName();

    /// <summary>
    /// Checks whether a user is currently authenticated.
    /// </summary>
    /// <returns><c>true</c> if user has valid JWT authentication; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Access HttpContext.User from IHttpContextAccessor.
    /// 2. Check Identity.IsAuthenticated property.
    /// 3. Return authentication status.
    /// </remarks>
    bool IsAuthenticated();

    /// <summary>
    /// Gets all role names assigned to the currently authenticated user.
    /// </summary>
    /// <returns>Collection of role names from JWT "role" claims.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Access HttpContext.User from IHttpContextAccessor.
    /// 2. Filter claims by type "role".
    /// 3. Extract role values.
    /// 4. Return collection (empty if no roles).
    /// </remarks>
    IReadOnlyCollection<string> GetUserRoles();

    /// <summary>
    /// Gets the IP address of the current request.
    /// </summary>
    /// <returns>Client IP address or null if not available.</returns>
    /// <remarks>
    /// Used for audit logging and security monitoring.
    /// End-to-end method steps:
    /// 1. Access HttpContext from IHttpContextAccessor.
    /// 2. Read RemoteIpAddress from connection info.
    /// 3. Handle proxy headers (X-Forwarded-For) if configured.
    /// 4. Return IP address string or null.
    /// </remarks>
    string? GetClientIpAddress();
}
