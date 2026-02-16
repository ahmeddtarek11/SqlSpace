using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Persistence;

/// <summary>
/// Repository abstraction for user-to-database access grants.
/// </summary>
/// <remarks>
/// Usage:
/// - Used by authorization and schema filtering to determine current effective access.
///
/// When:
/// - Before generating filtered schema or running query validation.
///
/// Why:
/// - Centralizes access lookup logic and soft-delete filtering in one place.
///
/// Where:
/// - Interface consumed by application services.
/// - Implementation belongs to Infrastructure persistence layer.
///
/// How:
/// - Query by connection id + user id for active (non-revoked/non-deleted) grant.
/// </remarks>
public interface IUserDatabaseAccessRepository
{
    /// <summary>
    /// Gets an active access grant for a specific user on a specific connection.
    /// </summary>
    /// <param name="connectionId">Connected database identifier.</param>
    /// <param name="userId">Application user identifier.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Active access grant when found; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query access rows by connection id and user id.
    /// 2. Exclude soft-deleted/revoked entries.
    /// 3. Return the active grant or <c>null</c>.
    /// </remarks>
    Task<UserDatabaseAccess?> GetActiveAccessAsync(Guid connectionId, string userId, CancellationToken cancellationToken);
}
