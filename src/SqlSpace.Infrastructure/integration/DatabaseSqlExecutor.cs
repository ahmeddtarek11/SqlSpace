using System.Data;
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Application.DTOs.Query;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.integration;

public class DatabaseSqlExecutor(ILogger<DatabaseSqlExecutor> logger ,  IDbConnectionFactory connectionFactory  )
 : IDatabaseSqlExecutor
{
    private readonly ILogger<DatabaseSqlExecutor> _logger = logger;
    //private readonly HybridCache _cache = cache;
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;

    public async Task<DatabaseQueryResult> ExecuteQueryAsync(ConnectedDatabase connection, string sql, CancellationToken cancellationToken)
    {
        const int maxRows = 2000;

        if(connection is null)
        {
            _logger.LogError("Connection can't be null ");
            return new DatabaseQueryResult
            {
                Success =false,
                ErrorMessage = "connection cannot be null try again with a valid connection instance"
            };
        }
        if (string.IsNullOrWhiteSpace(sql))
        {
             _logger.LogError("sql command passed is null or empty ");
            return new DatabaseQueryResult
            {
                 Success =false,
                ErrorMessage = "Sql command is null or empty , try again with proper sql command"
            };
        }

        var stopWatch = Stopwatch.StartNew();

        try
        {
            await using var dbConnection = await _connectionFactory.CreateOpenConnectionAsync(connection , cancellationToken);

            await using var command = dbConnection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 30;


            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            var columns = new List<string>();

            for(int i = 0; i < reader.FieldCount; i++)
            {
                columns.Add(reader.GetName(i));
            }

            // TODO(csv): implement streaming DbDataReader -> CSV writer path to avoid in-memory buffering for large exports.
            var rows = new List<object?[]>(maxRows);
            var isTruncated = false;

            while(await reader.ReadAsync(cancellationToken))
            {
                if (rows.Count == maxRows)
                {
                    // TODO(csv): when full result is requested and isTruncated=true, route execution to CSV export service.
                    isTruncated = true;
                    break;
                }

                var row = new object?[reader.FieldCount];
                for(int i=0;i<reader.FieldCount; i++)
                {
                    var value = reader.IsDBNull(i) ? null : reader.GetValue(i);
                    row[i] = value;
                }

                rows.Add(row);
            }

            stopWatch.Stop();

             var resultsJson = JsonSerializer.Serialize(new
        {
            columns,
            rows,
            isTruncated,
            maxRows
        }, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });

        return new DatabaseQueryResult
        {
            Success = true,
            ResultsJson = resultsJson,  
            RowsReturned = rows.Count,
            ExecutionTimeMs = stopWatch.ElapsedMilliseconds,
            ErrorMessage = null
        };
            
        }

       catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
    {
       stopWatch.Stop();  
    
        _logger.LogWarning(
        "Query execution cancelled for connection {ConnectionId}",
        connection.ConnectionId);

        return new DatabaseQueryResult  
        {
        Success = false,
        ErrorMessage = "Query execution was cancelled",
        ExecutionTimeMs = stopWatch.ElapsedMilliseconds
        };
      
    }
    catch (Exception ex)
    {
        _logger.LogError(
            ex,
            "Failed to execute query for connection {ConnectionId}",
            connection.ConnectionId);

        return new DatabaseQueryResult
        {
            Success = false,
            ErrorMessage = $"Query execution failed: {ex.Message}",
            ExecutionTimeMs = stopWatch.ElapsedMilliseconds
        };
    }
       
    }

    public async Task<ConnectionTestResult> TestConnectionAsync(ConnectedDatabase connection, CancellationToken cancellationToken)
    {
        if (connection is null)
        {
            _logger.LogError("Connection cannot be null for connection test.");
            return new ConnectionTestResult
            {
                Success = false,
                ErrorMessage = "Connection cannot be null.",
                ResponseTimeMs = 0
            };
        }

        var stopWatch = Stopwatch.StartNew();

        try
        {
            await using var dbConnection = await _connectionFactory.CreateOpenConnectionAsync(connection, cancellationToken);
            stopWatch.Stop();

            var testedDatabaseName = string.IsNullOrWhiteSpace(dbConnection.Database)
                ? connection.DatabaseName
                : dbConnection.Database;

            var responseTimeMs = stopWatch.ElapsedMilliseconds > int.MaxValue
                ? int.MaxValue
                : (int)stopWatch.ElapsedMilliseconds;

            return new ConnectionTestResult
            {
                Success = true,
                DatabaseName = testedDatabaseName,
                ServerVersion = dbConnection.ServerVersion,
                ResponseTimeMs = responseTimeMs,
                ErrorMessage = null
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopWatch.Stop();
            _logger.LogWarning(
                "Connection test cancelled for connection {ConnectionId}",
                connection.ConnectionId);

            return new ConnectionTestResult
            {
                Success = false,
                ErrorMessage = "Connection test was cancelled.",
                DatabaseName = connection.DatabaseName,
                ResponseTimeMs = stopWatch.ElapsedMilliseconds > int.MaxValue ? int.MaxValue : (int)stopWatch.ElapsedMilliseconds
            };
        }
        catch (Exception ex)
        {
            stopWatch.Stop();
            _logger.LogError(
                ex,
                "Connection test failed for connection {ConnectionId}",
                connection.ConnectionId);

            return new ConnectionTestResult
            {
                Success = false,
                ErrorMessage = $"Connection test failed: {ex.Message}",
                DatabaseName = connection.DatabaseName,
                ResponseTimeMs = stopWatch.ElapsedMilliseconds > int.MaxValue ? int.MaxValue : (int)stopWatch.ElapsedMilliseconds
            };
        }
    }
}
