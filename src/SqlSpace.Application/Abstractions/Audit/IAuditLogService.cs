using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.Abstractions.Audit;

/// <summary>
/// Records security-relevant actions to immutable audit trail for compliance and investigation.
/// </summary>
/// <remarks>
/// Usage:
/// - Called by access control and connection management services.
/// - Records all changes to user permissions and connection ownership.
///
/// When:
/// - User access is granted, updated, or revoked.
/// - Connection ownership is transferred.
/// - Security-relevant administrative actions occur.
///
/// Why:
/// - Provides immutable audit trail for compliance (SOC 2, GDPR, HIPAA).
/// - Enables security investigation and forensics.
/// - Answers questions: "Who granted access to whom and when?"
///
/// Where:
/// - Interface consumed by application services performing access control changes.
/// - Implemented in Infrastructure persistence layer.
///
/// How:
/// - Create AccessAuditLog records for each security action.
/// - Capture actor, target, action type, timestamp, and details.
/// - Never delete audit logs (immutable trail).
/// </remarks>
public interface IAuditLogRepository
{
    /// <summary>
    /// Logs a user being granted access to a database connection.
    /// </summary>
    /// <param name="connectionId">Database connection identifier.</param>
    /// <param name="actorUserId">User identifier of admin granting access.</param>
    /// <param name="targetUserId">User identifier of user receiving access.</param>
    /// <param name="hasFullAccess">Whether full access or restricted access was granted.</param>
    /// <param name="restrictedTables">List of restricted tables if applicable.</param>
    /// <param name="cancellationToken">Cancellation token for persistence operation.</param>
    /// <returns>A task that completes when log is persisted.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Create AccessAuditLog entity.
    /// 2. Set action to AccessGranted.
    /// 3. Serialize grant details to JSON (full vs restricted, table list).
    /// 4. Capture current timestamp.
    /// 5. Persist log record.
    /// 6. Complete operation.
    /// </remarks>
    Task<Result> LogAccessGrantedAsync(
        Guid connectionId,
        string actorUserId,
        string targetUserId,
        bool hasFullAccess,
        IReadOnlyList<string>? restrictedTables,
        CancellationToken cancellationToken);

    /// <summary>
    /// Logs user access restrictions being updated.
    /// </summary>
    /// <param name="connectionId">Database connection identifier.</param>
    /// <param name="actorUserId">User identifier of admin updating restrictions.</param>
    /// <param name="targetUserId">User identifier whose restrictions changed.</param>
    /// <param name="previousFullAccess">Previous access level.</param>
    /// <param name="newFullAccess">New access level.</param>
    /// <param name="previousRestrictions">Previous restricted tables.</param>
    /// <param name="newRestrictions">New restricted tables.</param>
    /// <param name="cancellationToken">Cancellation token for persistence operation.</param>
    /// <returns>A task that completes when log is persisted.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Create AccessAuditLog entity.
    /// 2. Set action to PermissionsUpdated.
    /// 3. Serialize before/after details to JSON for audit trail.
    /// 4. Capture current timestamp.
    /// 5. Persist log record.
    /// 6. Complete operation.
    /// </remarks>
    Task<Result> LogRestrictionsUpdatedAsync(
        Guid connectionId,
        string actorUserId,
        string targetUserId,
        bool previousFullAccess,
        bool newFullAccess,
        IReadOnlyList<string>? previousRestrictions,
        IReadOnlyList<string>? newRestrictions,
        CancellationToken cancellationToken);

    /// <summary>
    /// Logs a user's access to a connection being revoked.
    /// </summary>
    /// <param name="connectionId">Database connection identifier.</param>
    /// <param name="actorUserId">User identifier of admin revoking access.</param>
    /// <param name="targetUserId">User identifier whose access was revoked.</param>
    /// <param name="cancellationToken">Cancellation token for persistence operation.</param>
    /// <returns>A task that completes when log is persisted.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Create AccessAuditLog entity.
    /// 2. Set action to AccessRevoked.
    /// 3. Capture current timestamp.
    /// 4. Persist log record.
    /// 5. Complete operation.
    /// </remarks>
    Task<Result> LogAccessRevokedAsync(
        Guid connectionId,
        string actorUserId,
        string targetUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Logs database connection ownership transfer.
    /// </summary>
    /// <param name="connectionId">Database connection identifier.</param>
    /// <param name="previousAdminUserId">Previous connection owner.</param>
    /// <param name="newAdminUserId">New connection owner.</param>
    /// <param name="cancellationToken">Cancellation token for persistence operation.</param>
    /// <returns>A task that completes when log is persisted.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Create AccessAuditLog entity.
    /// 2. Set action to OwnershipTransferred.
    /// 3. Set actor as previous admin, target as new admin.
    /// 4. Capture current timestamp.
    /// 5. Persist log record.
    /// 6. Complete operation.
    /// </remarks>
    Task<Result> LogOwnershipTransferAsync(
        Guid connectionId,
        string previousAdminUserId,
        string newAdminUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves audit log entries for a connection with pagination.
    /// </summary>
    /// <param name="connectionId">Database connection identifier.</param>
    /// <param name="pageNumber">Page number (1-based).</param>
    /// <param name="pageSize">Number of records per page.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Paginated list of audit log entries ordered by timestamp descending.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query AccessAuditLog by connection id.
    /// 2. Order by PerformedAt descending (newest first).
    /// 3. Apply pagination (skip/take).
    /// 4. Include actor and target user details.
    /// 5. Return page result with total count.
    /// </remarks>
    Task<Result<PaginatedAuditLogs>> GetConnectionAuditLogsAsync(
        Guid connectionId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken);
}
