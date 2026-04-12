using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Access;

/// <summary>
/// Manages user access grants and table-level restrictions for database connections.
/// </summary>
/// <remarks>
/// Usage:
/// - Used by admin users to grant, update, and revoke access to database connections.
/// - Used by query execution to verify user authorization before SQL execution.
///
/// When:
/// - Admin adds user to a connection from access management UI.
/// - Admin modifies table restrictions for a user.
/// - Query execution validates user can access tables referenced in SQL.
///
/// Why:
/// - Centralizes all access control logic and permission checks.
/// - Ensures consistent authorization enforcement across the application.
/// - Maintains audit trail of access changes through integration with audit service.
///
/// Where:
/// - Interface consumed by API controllers and query execution workflows.
/// - Implemented in Application layer as a use-case orchestration service.
///
/// How:
/// - Query and manipulate UserDatabaseAccess and TableRestriction entities.
/// - Coordinate with audit log service for access change tracking.
/// - Resolve effective permissions (full access vs restricted with exclusions).
/// </remarks>
public interface IAccessControlService
{
    /// <summary>
    /// Grants database connection access to a user with optional table restrictions.
    /// </summary>
    /// <param name="connectionId">Target database connection identifier.</param>
    /// <param name="adminUserId">Admin user identifier granting the access (must be connection owner).</param>
    /// <param name="targetUserEmail">Email address of user receiving access.</param>
    /// <param name="hasFullAccess">Whether user gets full access (all tables) or restricted access.</param>
    /// <param name="restrictedTables">List of table names to exclude (only used when hasFullAccess is false).</param>
    /// <param name="cancellationToken">Cancellation token for operation lifetime control.</param>
    /// <returns>Created access grant entity with assigned identifier.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate admin user owns the connection.
    /// 2. Resolve target user by email address.
    /// 3. Check target user doesn't already have active access.
    /// 4. Create UserDatabaseAccess record.
    /// 5. If restricted access, create TableRestriction records for each excluded table.
    /// 6. Persist changes atomically.
    /// 7. Log access grant to audit trail.
    /// 8. (Optional) Send notification email to target user.
    /// 9. Return created access entity.
    /// </remarks>
    Task<Result<UserAccessSummary>> GrantAccessAsync(
        Guid connectionId,
        string adminUserId,
        string targetUserEmail,
        bool hasFullAccess,
        IReadOnlyList<TableRestrictionInput>? restrictedTables,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates table restrictions for an existing user access grant.
    /// </summary>
    /// <param name="connectionId">Target database connection identifier.</param>
    /// <param name="adminUserId">Admin user identifier making the change (must be connection owner).</param>
    /// <param name="targetUserId">User identifier whose restrictions are being modified.</param>
    /// <param name="hasFullAccess">Updated access level (full or restricted).</param>
    /// <param name="restrictedTables">Updated list of excluded tables (null if full access).</param>
    /// <param name="cancellationToken">Cancellation token for operation lifetime control.</param>
    /// <returns>A task that completes when restrictions are updated.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate admin user owns the connection.
    /// 2. Load existing access grant for target user.
    /// 3. Update HasFullAccess flag.
    /// 4. Replace all existing TableRestriction records with new set.
    /// 5. Persist changes atomically.
    /// 6. Log restriction update to audit trail.
    /// 7. Complete operation.
    /// </remarks>
    Task<Result> UpdateAccessRestrictionsAsync(
        Guid connectionId,
        string adminUserId,
        string targetUserId,
        bool hasFullAccess,
        IReadOnlyList<TableRestrictionInput>? restrictedTables,
        CancellationToken cancellationToken);

    /// <summary>
    /// Revokes a user's access to a database connection.
    /// </summary>
    /// <param name="connectionId">Target database connection identifier.</param>
    /// <param name="adminUserId">Admin user identifier revoking the access (must be connection owner).</param>
    /// <param name="targetUserId">User identifier whose access is being revoked.</param>
    /// <param name="cancellationToken">Cancellation token for operation lifetime control.</param>
    /// <returns><c>true</c> if access was revoked; <c>false</c> if access didn't exist.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate admin user owns the connection.
    /// 2. Load active access grant for target user.
    /// 3. Soft-delete access by setting IsDeleted = true and RevokedAt timestamp.
    /// 4. Set RevokedByUserId for audit trail.
    /// 5. Persist changes.
    /// 6. Log access revocation to audit trail.
    /// 7. Return success indicator.
    /// </remarks>
    Task<Result<bool>> RevokeAccessAsync(
        Guid connectionId,
        string adminUserId,
        string targetUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a user has access to a database connection.
    /// </summary>
    /// <param name="connectionId">Database connection identifier to check.</param>
    /// <param name="userId">User identifier to check access for.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns><c>true</c> if user is admin or has active access grant; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load connection metadata.
    /// 2. Check if user is connection admin (implicit access).
    /// 3. If not admin, query for active UserDatabaseAccess grant.
    /// 4. Return true if admin or active grant exists.
    /// </remarks>
    Task<Result<bool>> HasAccessToConnectionAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a user can access a specific table in a database connection.
    /// </summary>
    /// <param name="connectionId">Database connection identifier.</param>
    /// <param name="userId">User identifier to check permissions for.</param>
    /// <param name="tableName">Table name to check access for.</param>
    /// <param name="schemaName">Optional schema name qualifier (e.g., "dbo", "public").</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns><c>true</c> if user can access the table; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load connection metadata.
    /// 2. Check if user is connection admin (full implicit access).
    /// 3. If not admin, load user's access grant.
    /// 4. If HasFullAccess = true, return true.
    /// 5. If restricted access, check TableRestriction records.
    /// 6. Return false if table is in restriction list, true otherwise.
    /// </remarks>
    Task<Result<bool>> CanAccessTableAsync(
        Guid connectionId,
        string userId,
        string tableName,
        string? schemaName,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the list of table names a user is authorized to query.
    /// </summary>
    /// <param name="connectionId">Database connection identifier.</param>
    /// <param name="userId">User identifier to get accessible tables for.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>List of table names user can access (empty if no access).</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load latest schema snapshot for connection.
    /// 2. Parse schema JSON to extract all table names.
    /// 3. Check if user is admin (return all tables).
    /// 4. If not admin, load user's access grant.
    /// 5. If HasFullAccess = true, return all tables.
    /// 6. If restricted, load TableRestrictions.
    /// 7. Filter out restricted tables from full list.
    /// 8. Return filtered accessible table list.
    /// </remarks>
    Task<Result<ICollection<string>>> GetAccessibleTableNamesAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Lists all users with access to a specific database connection.
    /// </summary>
    /// <param name="connectionId">Database connection identifier.</param>
    /// <param name="adminUserId">Admin user identifier requesting the list (must be connection owner).</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>List of user access grants with restriction details.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate requesting user is connection admin.
    /// 2. Query active UserDatabaseAccess records for connection.
    /// 3. Join with ApplicationUser to get user details.
    /// 4. For each restricted access, load associated TableRestrictions.
    /// 5. Return list of access grants with user and restriction info.
    /// </remarks>
    Task<Result<ICollection<UserAccessSummary>>> ListConnectionUsersAsync(
        Guid connectionId,
        string adminUserId,
        CancellationToken cancellationToken);

        Task<Result<bool>> IsAdmin(Guid ConnectionId , string userId);

    
}
