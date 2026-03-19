using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using MySqlConnector;
using Npgsql;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.Abstractions.Security;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.Connection;

public class DbConnectionFactory(IEncryptionService encryptionService,
                               ILogger<DbConnectionFactory> logger, IConnectionStringBuilder connectionStringBuilder) : IDbConnectionFactory
{
    private readonly IEncryptionService _encryptionService = encryptionService;
    private readonly ILogger<DbConnectionFactory> _logger = logger;
    private readonly IConnectionStringBuilder _connectionStringBuilder = connectionStringBuilder;

    public async Task<DbConnection> CreateOpenConnectionAsync(ConnectedDatabase connection, CancellationToken cancellationToken)
    {

         if (connection == null)
        throw new ArgumentNullException(nameof(connection));

        if (connection.UsesRawConnectionString &&
            string.IsNullOrWhiteSpace(connection.EncryptedRawConnectionString))
        {
            throw new InvalidOperationException(
                $"Connection {connection.ConnectionId} is configured for raw connection string mode but has no encrypted raw connection string.");
        }

        if (!connection.UsesRawConnectionString)
        {
            if (string.IsNullOrWhiteSpace(connection.EncryptedPassword))
            {
                throw new InvalidOperationException(
                    $"Connection {connection.ConnectionId} is missing encrypted password for component connection mode.");
            }

            if (string.IsNullOrWhiteSpace(connection.Host))
            {
                throw new InvalidOperationException(
                    $"Connection {connection.ConnectionId} is missing host for component connection mode.");
            }

            if (string.IsNullOrWhiteSpace(connection.Username))
            {
                throw new InvalidOperationException(
                    $"Connection {connection.ConnectionId} is missing username for component connection mode.");
            }

            if (string.IsNullOrWhiteSpace(connection.DatabaseName))
            {
                throw new InvalidOperationException(
                    $"Connection {connection.ConnectionId} is missing database name for component connection mode.");
            }
        }

        string connectionString;
        try
        {

            if (connection.UsesRawConnectionString)
            {

                connectionString = _encryptionService.Decrypt(connection.EncryptedRawConnectionString!);

            }
            else
            {
                var connectionPassword = _encryptionService.Decrypt(connection.EncryptedPassword!);
                int port = connection.PortNumber ?? connection.DatabaseProvider.GetDefaultPort();


                connectionString = _connectionStringBuilder.BuildConnectionString(connection.DatabaseProvider,
                                                                               connection.Host!,
                                                                               port,
                                                                               connection.DatabaseName,
                                                                               connection.Username!,
                                                                               connectionPassword,
                                                                               connection.UseSSL,
                                                                               connection.AdditionalParameters);

            }

        }
        catch (Exception ex)
        {
            _logger.LogError(
            ex,
            "Failed to build connection string for connection {ConnectionId}",
            connection.ConnectionId);
            throw;
        }


        DbConnection dbConnection = connection.DatabaseProvider switch
        {
            DbProviders.PostgreSql  => new NpgsqlConnection(connectionString),
            DbProviders.CockroachDb => new NpgsqlConnection(connectionString),
            DbProviders.Supabase    => new NpgsqlConnection(connectionString),
            DbProviders.Redshift    => new NpgsqlConnection(connectionString),
            DbProviders.SqlServer   => new SqlConnection(connectionString),
            DbProviders.MySql       => new MySqlConnection(connectionString),
            DbProviders.MariaDb     => new MySqlConnection(connectionString),
            DbProviders.PlanetScale => new MySqlConnection(connectionString),
            _ => throw new NotSupportedException($"Database Provider {connection.DatabaseProvider} is not supported yet")
        };


        try
        {
            await dbConnection.OpenAsync(cancellationToken);

            _logger.LogDebug("Successfully opened a connection to {provider} database {database} for connection {connectionId} .",
                             connection.DatabaseProvider,
                             connection.DatabaseName,
                             connection.ConnectionId);

            return dbConnection;
        }

        catch (OperationCanceledException)
        {
            try
            {
                await dbConnection.DisposeAsync();
            }
            catch (Exception disposeEx)
            {
                _logger.LogWarning(
                    disposeEx,
                    "Failed to dispose connection after cancellation for connection {ConnectionId}.",
                    connection.ConnectionId);
            }

            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
            ex,
            "Failed to open connection to {Provider} database {Database} for connection {ConnectionId}",
            connection.DatabaseProvider,
            connection.DatabaseName,
            connection.ConnectionId);

            try
            {
                await dbConnection.DisposeAsync();
            }
            catch (Exception disposeEx)
            {
                // Log but don't let dispose failure hide the real problem
                _logger.LogWarning(
                    disposeEx,
                    "Failed to dispose connection after open failure. Original error was: {OriginalError}",
                    ex.Message);
            }
            
            throw;  
        }


    
    }
}
