using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Persistence;

/// <summary>
/// Repository abstraction for reading persisted connected-database metadata.
/// </summary>
/// <remarks>
/// Usage:
/// - Used by schema/query workflows to resolve connection ownership and provider details.
///
/// When:
/// - Before opening external DB connections or refreshing schema snapshots.
///
/// Why:
/// - Keeps storage query logic centralized and reusable.
///
/// Where:
/// - Interface consumed by application services.
/// - Implementation belongs to Infrastructure persistence layer.
///
/// How:
/// - Query the persistence store by connection identifier.
/// </remarks>
public interface IConnectedDatabaseRepository
{
    /// <summary>
    /// Gets a single connected database by its unique identifier.
    /// </summary>
    /// <param name="connectionId">Connection identifier.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>The connection when found; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Build query by connection id.
    /// 2. Execute query against persistence store.
    /// 3. Materialize entity.
    /// 4. Return entity or <c>null</c>.
    /// </remarks>
    Task<ConnectedDatabase?> GetByIdAsync(Guid connectionId, CancellationToken cancellationToken);
}
