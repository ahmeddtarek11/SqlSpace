using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Schema;

/// <summary>
/// Extracts a normalized schema representation from a live external database.
/// </summary>
/// <remarks>
/// Usage:
/// - Used by schema refresh workflows.
///
/// When:
/// - On first schema capture and on explicit refresh operations.
///
/// Why:
/// - Provides a consistent schema JSON payload for LLM context and schema versioning.
///
/// Where:
/// - Interface consumed by schema context service.
/// - Implementation belongs to Infrastructure and is provider-aware.
///
/// How:
/// - Connect to target DB using connection factory.
/// - Query metadata/catalog views.
/// - Shape metadata into standardized JSON output.
/// </remarks>
public interface ISchemaExtractor
{
    /// <summary>
    /// Reads the current live database metadata and returns schema as JSON.
    /// </summary>
    /// <param name="connection">Connected database metadata and credentials.</param>
    /// <param name="cancellationToken">Cancellation token for metadata queries.</param>
    /// <returns>Normalized schema JSON string for downstream processing.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate connection metadata.
    /// 2. Open provider-specific DB connection.
    /// 3. Query system metadata (schemas/tables/columns).
    /// 4. Transform metadata to shared JSON contract.
    /// 5. Return serialized schema text.
    /// </remarks>
    Task<string> ExtractSchemaJsonAsync(ConnectedDatabase connection, CancellationToken cancellationToken);
}
