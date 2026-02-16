using System.Data.Common;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Abstractions.Integrations;

/// <summary>
/// Creates and opens provider-specific database connections to external user-managed databases.
/// </summary>
/// <remarks>
/// Usage:
/// - Called by schema extraction and query execution workflows.
///
/// When:
/// - Anytime SqlSpace needs a live connection to PostgreSQL/SQL Server/MySQL.
///
/// Why:
/// - Encapsulates provider-specific connection logic away from business workflows.
///
/// Where:
/// - Interface consumed by application services.
/// - Implementation belongs to Infrastructure integration layer.
///
/// How:
/// - Resolve provider from connection metadata.
/// - Build native connection string.
/// - Open and return ready-to-use <see cref="DbConnection"/>.
/// </remarks>
public interface IDbConnectionFactory
{
    /// <summary>
    /// Creates and opens an external database connection from stored connection metadata.
    /// </summary>
    /// <param name="connection">Connected database metadata (provider, host, credentials, options).</param>
    /// <param name="cancellationToken">Cancellation token for connection open operation.</param>
    /// <returns>An open provider-specific <see cref="DbConnection"/>.</returns>
    /// <remarks>
    /// End-to-end method steps:
    /// 1. Validate required connection metadata.
    /// 2. Select provider-specific ADO.NET connection type.
    /// 3. Build final connection string.
    /// 4. Open connection asynchronously.
    /// 5. Return open connection to caller.
    /// </remarks>
    Task<DbConnection> CreateOpenConnectionAsync(ConnectedDatabase connection, CancellationToken cancellationToken);
}
