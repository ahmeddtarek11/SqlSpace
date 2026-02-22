namespace SqlSpace.Application.Abstractions.Schema;

/// <summary>
/// Builds and refreshes the schema context consumed by the LLM SQL-generation pipeline.
/// </summary>
/// <remarks>
/// Usage:
/// - Called by natural-language query workflows before sending prompt to FastAPI.
///
/// When:
/// - For every query generation request and manual schema refresh operation.
///
/// Why:
/// - Ensures LLM sees only schema the user is allowed to access.
/// - Applies cache-first strategy for performance.
///
/// Where:
/// - Interface consumed by application query orchestration.
/// - Implemented in Application layer as a schema-context orchestration service.
/// How:
/// - Load latest cached snapshot.
/// - Resolve user access and restrictions.
/// - Return full or filtered schema JSON, or user override when provided.
/// </remarks>
public interface ISchemaContextService
{
    /// <summary>
    /// Returns schema JSON used for prompt-to-SQL generation, filtered by user access rules.
    /// </summary>
    /// <param name="connectionId">Connected database identifier.</param>
    /// <param name="userId">Current user identifier.</param>
    /// <param name="userProvidedSchemaOverride">Optional manual schema text supplied by caller.</param>
    /// <param name="cancellationToken">Cancellation token for I/O operations.</param>
    /// <returns>Schema JSON string to send to the AI service.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. If manual override exists, return it immediately.
    /// 2. Load latest schema snapshot from cache (DatabaseSchemaSnapshot, IsLatest=true).
    /// 3. If missing, trigger refresh and retry snapshot load.
    /// 4. Load user's active access grant (UserDatabaseAccess).
    /// 5. If user is admin, return snapshot as-is.
    /// 6. If HasFullAccess=true, return snapshot as-is.
    /// 7. If restricted access, load TableRestrictions.
    /// 8. Deserialize schema JSON, remove excluded tables, serialize filtered JSON.
    /// 9. Return filtered schema.
    /// </remarks>
    Task<string> GetFilteredSchemaForPromptAsync(
        Guid connectionId,
        string userId,
        string? userProvidedSchemaOverride,
        CancellationToken cancellationToken);

    /// <summary>
    /// Refreshes stored schema snapshot for a connection by re-reading live external schema.
    /// </summary>
    /// <param name="connectionId">Connected database identifier.</param>
    /// <param name="requestedByUserId">User id requesting refresh (for audit/log usage).</param>
    /// <param name="cancellationToken">Cancellation token for I/O operations.</param>
    /// <returns>A task that completes when snapshot update is finished.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Load connected database metadata.
    /// 2. Open external connection using IDbConnectionFactory.
    /// 3. Extract live schema using ISchemaExtractor.
    /// 4. Compute schema hash (MD5 or SHA256).
    /// 5. Load current latest snapshot.
    /// 6. Compare hashes to detect changes.
    /// 7. If changed:
    ///    - Set existing snapshots IsLatest=false
    ///    - Create new DatabaseSchemaSnapshot with IsLatest=true
    ///    - Persist changes
    /// 8. If unchanged, complete without creating new snapshot.
    /// </remarks>
    Task RefreshSchemaAsync(
        Guid connectionId, 
        string requestedByUserId, 
        CancellationToken cancellationToken);
}
