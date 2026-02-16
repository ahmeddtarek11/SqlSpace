using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Persistence;

/// <summary>
/// Repository abstraction for table-level exclusion rules attached to a user's database access.
/// </summary>
/// <remarks>
/// Usage:
/// - Used by authorization and schema-filtering flows.
/// - Restrictions represent tables a restricted user cannot query.
///
/// When:
/// - During access grant updates and before query/schema generation.
///
/// Why:
/// - Guarantees table-level policy is consistently applied across query validation and LLM context filtering.
///
/// Where:
/// - Interface consumed by application services.
/// - Implementation belongs to Infrastructure persistence layer.
///
/// How:
/// - Persist and retrieve exclusions by <c>UserDatabaseAccessId</c>.
/// - Match schema-qualified and non-qualified table names.
///
/// Note:
/// - Full-access users ignore this repository's restrictions at runtime.
/// </remarks>
public interface IRestrictionsRepository
{
    /// <summary>
    /// Lists all restricted tables for a single user-access entry.
    /// </summary>
    /// <param name="userDatabaseAccessId">Access record id that owns the restrictions.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns>Collection of table restrictions for that access record.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Query restrictions by <paramref name="userDatabaseAccessId"/>.
    /// 2. Materialize list ordered as needed by implementation.
    /// 3. Return empty list when none exist.
    /// </remarks>
    Task<IReadOnlyList<TableRestriction>> ListByAccessIdAsync(int userDatabaseAccessId, CancellationToken cancellationToken);

    /// <summary>
    /// Replaces all restrictions for a user-access entry with a new set.
    /// </summary>
    /// <param name="userDatabaseAccessId">Access record id that will receive the new restrictions.</param>
    /// <param name="restrictions">New full restriction set to persist.</param>
    /// <param name="cancellationToken">Cancellation token for persistence operations.</param>
    /// <returns>A task that completes after replacement is persisted.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load current restrictions for the access id.
    /// 2. Remove existing rows.
    /// 3. Insert provided replacement rows.
    /// 4. Persist transaction atomically.
    /// </remarks>
    Task ReplaceAsync(int userDatabaseAccessId, IReadOnlyCollection<TableRestriction> restrictions, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a specific table is blocked for a user-access entry.
    /// </summary>
    /// <param name="userDatabaseAccessId">Access record id to check against.</param>
    /// <param name="schemaName">Optional schema name, such as <c>dbo</c> or <c>public</c>.</param>
    /// <param name="tableName">Table name to evaluate.</param>
    /// <param name="cancellationToken">Cancellation token for query execution.</param>
    /// <returns><c>true</c> if restricted; otherwise <c>false</c>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Normalize input table key (schema + table).
    /// 2. Query matching restriction rows.
    /// 3. Return match result.
    /// </remarks>
    Task<bool> IsRestrictedAsync(int userDatabaseAccessId, string? schemaName, string tableName, CancellationToken cancellationToken);
}
