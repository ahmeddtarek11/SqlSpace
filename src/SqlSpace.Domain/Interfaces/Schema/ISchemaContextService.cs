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
/// - Implementation typically in Application layer, using persistence + extraction abstractions.
///
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
    /// 2. Load latest schema snapshot from cache.
    /// 3. If missing, trigger refresh and retry snapshot load.
    /// 4. Load user's active access grant.
    /// 5. If full access, return snapshot as-is.
    /// 6. If restricted access, remove excluded tables and return filtered JSON.
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
    /// 2. Open external connection and extract live schema.
    /// 3. Compute schema hash.
    /// 4. Compare against current latest snapshot.
    /// 5. If changed, insert new snapshot and mark it latest.
    /// 6. Complete without change when hash is identical.
    /// </remarks>
    Task RefreshSchemaAsync(Guid connectionId, string requestedByUserId, CancellationToken cancellationToken);
}
