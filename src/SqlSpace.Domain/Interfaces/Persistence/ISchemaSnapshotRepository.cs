using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Persistence;

/// <summary>
/// Repository abstraction for schema snapshot persistence and version selection.
/// </summary>
/// <remarks>
/// Usage:
/// - Used by schema-context service for cache-first schema retrieval.
///
/// When:
/// - On query generation (read latest snapshot) and schema refresh operations.
///
/// Why:
/// - Preserves historical schema versions while identifying the active snapshot.
///
/// Where:
/// - Interface consumed by application services.
/// - Implementation belongs to Infrastructure persistence layer.
///
/// How:
/// - Store schema text/hash per connection and manage <c>IsLatest</c> marker.
/// </remarks>
public interface ISchemaSnapshotRepository
{
    /// <summary>
    /// Gets the latest schema snapshot for a connection.
    /// </summary>
    /// <param name="connectionId">Connected database identifier.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Latest snapshot when found; otherwise <c>null</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query snapshots for the connection.
    /// 2. Filter to latest marker (or newest by timestamp depending on implementation).
    /// 3. Return snapshot or <c>null</c>.
    /// </remarks>
    Task<DatabaseSchemaSnapshot?> GetLatestAsync(Guid connectionId, CancellationToken cancellationToken);

    /// <summary>
    /// Adds a new schema snapshot record.
    /// </summary>
    /// <param name="snapshot">Snapshot entity containing schema text/hash and metadata.</param>
    /// <param name="cancellationToken">Cancellation token for persistence operation.</param>
    /// <returns>A task that completes when insertion succeeds.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate snapshot payload.
    /// 2. Insert new row.
    /// 3. Persist changes.
    /// </remarks>
    Task AddAsync(DatabaseSchemaSnapshot snapshot, CancellationToken cancellationToken);

    /// <summary>
    /// Marks one snapshot as latest for a connection and clears latest marker from others.
    /// </summary>
    /// <param name="connectionId">Connected database identifier.</param>
    /// <param name="snapshotId">Snapshot identifier to mark as latest.</param>
    /// <param name="cancellationToken">Cancellation token for persistence operation.</param>
    /// <returns>A task that completes after latest marker update.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Find snapshots for the connection.
    /// 2. Set <c>IsLatest = false</c> on previous latest rows.
    /// 3. Set <c>IsLatest = true</c> on target snapshot.
    /// 4. Persist changes atomically.
    /// </remarks>
    Task MarkLatestAsync(Guid connectionId, Guid snapshotId, CancellationToken cancellationToken);
}
