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
/// - Implemented in Infrastructure and is provider-aware.
///
/// How:
/// - Connect to target DB using connection factory.
/// - Query metadata/catalog views (INFORMATION_SCHEMA).
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
    /// 2. Open provider-specific DB connection via IDbConnectionFactory.
    /// 3. Query system metadata:
    ///    - PostgreSQL: INFORMATION_SCHEMA or pg_catalog
    ///    - SQL Server: INFORMATION_SCHEMA or sys views
    ///    - MySQL: INFORMATION_SCHEMA
    /// 4. Extract for each table:
    ///    - Schema name (e.g., "public", "dbo")
    ///    - Table name
    ///    - Table type (TABLE, VIEW)
    ///    - Columns: name, data type, nullable, primary key, max length
    /// 5. Transform metadata to shared JSON contract:
    ///    {
    ///      "database": "dbname",
    ///      "capturedAt": "2026-02-17T12:00:00Z",
    ///      "tables": [
    ///        {
    ///          "schema": "public",
    ///          "name": "users",
    ///          "type": "TABLE",
    ///          "columns": [
    ///            {
    ///              "name": "id",
    ///              "dataType": "integer",
    ///              "isPrimaryKey": true,
    ///              "isNullable": false,
    ///              "maxLength": null
    ///            }
    ///          ]
    ///        }
    ///      ]
    ///    }
    /// 6. Serialize to JSON string.
    /// 7. Return schema text.
    /// </remarks>
    Task<string> ExtractSchemaJsonAsync(
        ConnectedDatabase connection, 
        CancellationToken cancellationToken);
}
