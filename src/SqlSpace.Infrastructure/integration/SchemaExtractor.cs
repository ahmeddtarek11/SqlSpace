using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.Abstractions.Schema;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Infrastructure.integration;

public class SchemaExtractor(IDbConnectionFactory connectionFactory, ILogger<SchemaExtractor> logger) : ISchemaExtractor
{
    private readonly IDbConnectionFactory _connectionFactory = connectionFactory;
    private readonly ILogger<SchemaExtractor> _logger = logger;

    public async Task<string> ExtractSchemaJsonAsync(ConnectedDatabase connection, CancellationToken cancellationToken)
    {
        if (connection is null)
        {
            _logger.LogError("Schema Extraction Failed, Database instance is null");
            throw new ArgumentNullException(nameof(connection));
        }

        try
        {
            await using var openConnection = await _connectionFactory.CreateOpenConnectionAsync(connection, cancellationToken);

            await using var command = openConnection.CreateCommand();
            command.CommandText = GetSchemaQuery(connection.DatabaseProvider);
            command.CommandType = CommandType.Text;
            command.CommandTimeout = 60;

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            var databaseName = string.IsNullOrWhiteSpace(openConnection.Database)
                ? connection.DatabaseName
                : openConnection.Database;

            var snapshot = new SchemaSnapshotPayload
            {
                Database = databaseName ?? string.Empty,
                CapturedAt = DateTime.UtcNow,
                Tables = new List<SchemaTablePayload>()
            };

            var tablesByKey = new Dictionary<string, SchemaTablePayload>(StringComparer.OrdinalIgnoreCase);

            while (await reader.ReadAsync(cancellationToken))
            {
                var rawSchema = reader.IsDBNull(0) ? null : reader.GetString(0);
                var schema = connection.DatabaseProvider.NormalizeSchema(rawSchema);
                var tableName = reader.IsDBNull(1) ? string.Empty : reader.GetString(1);
                var rawType = reader.IsDBNull(2) ? string.Empty : reader.GetString(2);
                var columnName = reader.IsDBNull(3) ? string.Empty : reader.GetString(3);
                var dataType = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                var isNullableText = reader.IsDBNull(5) ? string.Empty : reader.GetString(5);

                var maxLength = ReadNullableInt(reader, 6);
                var isPrimaryKey = ReadNullableBool(reader, 7);
                var foreignKeyName = ReadNullableString(reader, 8);
                var referencedTableSchema = ReadNullableString(reader, 9);
                var referencedTableName = ReadNullableString(reader, 10);
                var referencedColumnName = ReadNullableString(reader, 11);

                var normalizedType = NormalizeTableType(rawType);
                var tableKey = connection.DatabaseProvider.BuildTableKey(tableName, schema);

                if (!tablesByKey.TryGetValue(tableKey, out var table))
                {
                    table = new SchemaTablePayload
                    {
                        Schema = schema,
                        Name = tableName,
                        Type = normalizedType,
                        Columns = new List<SchemaColumnPayload>()
                    };

                    tablesByKey[tableKey] = table;
                    snapshot.Tables.Add(table);
                }

                table.Columns.Add(new SchemaColumnPayload
                {
                    Name = columnName,
                    DataType = dataType,
                    IsPrimaryKey = isPrimaryKey,
                    IsNullable = string.Equals(isNullableText, "YES", StringComparison.OrdinalIgnoreCase),
                    MaxLength = maxLength,
                    ForeignKeyName = foreignKeyName,
                    ReferencedTableSchema = referencedTableSchema,
                    ReferencedTableName = referencedTableName,
                    ReferencedColumnName = referencedColumnName
                });
            }

            return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false
            });
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("Schema extraction cancelled for connection {ConnectionId}", connection.ConnectionId);
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Schema extraction failed for connection {ConnectionId}",
                connection.ConnectionId);
            throw;
        }
    }

    private static string GetSchemaQuery(DbProviders provider)
    {
        return provider switch
        {
            DbProviders.PostgreSql => @"
            SELECT
                c.table_schema,
                c.table_name,
                t.table_type,
                c.column_name,
                c.data_type,
                c.is_nullable,
                c.character_maximum_length,
                CASE WHEN pk.column_name IS NOT NULL THEN 1 ELSE 0 END AS is_primary_key,
                fk.constraint_name AS foreign_key_name,
                fk.referenced_table_schema,
                fk.referenced_table_name,
                fk.referenced_column_name
            FROM information_schema.columns c
            JOIN information_schema.tables t
            ON c.table_schema = t.table_schema
            AND c.table_name = t.table_name
            LEFT JOIN (
                SELECT kcu.table_schema, kcu.table_name, kcu.column_name
                FROM information_schema.table_constraints tc
                JOIN information_schema.key_column_usage kcu
                ON tc.constraint_name = kcu.constraint_name
                AND tc.table_schema = kcu.table_schema
                AND tc.table_name = kcu.table_name
                WHERE tc.constraint_type = 'PRIMARY KEY'
            ) pk
            ON c.table_schema = pk.table_schema
            AND c.table_name = pk.table_name
            AND c.column_name = pk.column_name
            LEFT JOIN (
                SELECT
                    fk_kcu.table_schema,
                    fk_kcu.table_name,
                    fk_kcu.column_name,
                    fk_tc.constraint_name,
                    pk_kcu.table_schema AS referenced_table_schema,
                    pk_kcu.table_name AS referenced_table_name,
                    pk_kcu.column_name AS referenced_column_name
                FROM information_schema.table_constraints fk_tc
                JOIN information_schema.key_column_usage fk_kcu
                ON fk_tc.constraint_catalog = fk_kcu.constraint_catalog
                AND fk_tc.constraint_schema = fk_kcu.constraint_schema
                AND fk_tc.constraint_name = fk_kcu.constraint_name
                AND fk_tc.table_schema = fk_kcu.table_schema
                AND fk_tc.table_name = fk_kcu.table_name
                JOIN information_schema.referential_constraints rc
                ON fk_tc.constraint_catalog = rc.constraint_catalog
                AND fk_tc.constraint_schema = rc.constraint_schema
                AND fk_tc.constraint_name = rc.constraint_name
                JOIN information_schema.key_column_usage pk_kcu
                ON pk_kcu.constraint_catalog = rc.unique_constraint_catalog
                AND pk_kcu.constraint_schema = rc.unique_constraint_schema
                AND pk_kcu.constraint_name = rc.unique_constraint_name
                AND pk_kcu.ordinal_position = fk_kcu.position_in_unique_constraint
                WHERE fk_tc.constraint_type = 'FOREIGN KEY'
            ) fk
            ON c.table_schema = fk.table_schema
            AND c.table_name = fk.table_name
            AND c.column_name = fk.column_name
            WHERE c.table_schema NOT IN ('pg_catalog', 'information_schema')
            ORDER BY c.table_schema, c.table_name, c.ordinal_position;",



            DbProviders.SqlServer => @"
            SELECT
                c.TABLE_SCHEMA,
                c.TABLE_NAME,
                t.TABLE_TYPE,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.CHARACTER_MAXIMUM_LENGTH,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS is_primary_key,
                fk.CONSTRAINT_NAME AS foreign_key_name,
                fk.REFERENCED_TABLE_SCHEMA,
                fk.REFERENCED_TABLE_NAME,
                fk.REFERENCED_COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS c
            JOIN INFORMATION_SCHEMA.TABLES t
            ON c.TABLE_SCHEMA = t.TABLE_SCHEMA
            AND c.TABLE_NAME = t.TABLE_NAME
            LEFT JOIN (
                SELECT kcu.TABLE_SCHEMA, kcu.TABLE_NAME, kcu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                AND tc.TABLE_NAME = kcu.TABLE_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk
            ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
            AND c.TABLE_NAME = pk.TABLE_NAME
            AND c.COLUMN_NAME = pk.COLUMN_NAME
            LEFT JOIN (
                SELECT
                    fk_kcu.TABLE_SCHEMA,
                    fk_kcu.TABLE_NAME,
                    fk_kcu.COLUMN_NAME,
                    fk_tc.CONSTRAINT_NAME,
                    pk_kcu.TABLE_SCHEMA AS REFERENCED_TABLE_SCHEMA,
                    pk_kcu.TABLE_NAME AS REFERENCED_TABLE_NAME,
                    pk_kcu.COLUMN_NAME AS REFERENCED_COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS fk_tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE fk_kcu
                ON fk_tc.CONSTRAINT_CATALOG = fk_kcu.CONSTRAINT_CATALOG
                AND fk_tc.CONSTRAINT_SCHEMA = fk_kcu.CONSTRAINT_SCHEMA
                AND fk_tc.CONSTRAINT_NAME = fk_kcu.CONSTRAINT_NAME
                AND fk_tc.TABLE_SCHEMA = fk_kcu.TABLE_SCHEMA
                AND fk_tc.TABLE_NAME = fk_kcu.TABLE_NAME
                JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS rc
                ON fk_tc.CONSTRAINT_CATALOG = rc.CONSTRAINT_CATALOG
                AND fk_tc.CONSTRAINT_SCHEMA = rc.CONSTRAINT_SCHEMA
                AND fk_tc.CONSTRAINT_NAME = rc.CONSTRAINT_NAME
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE pk_kcu
                ON pk_kcu.CONSTRAINT_CATALOG = rc.UNIQUE_CONSTRAINT_CATALOG
                AND pk_kcu.CONSTRAINT_SCHEMA = rc.UNIQUE_CONSTRAINT_SCHEMA
                AND pk_kcu.CONSTRAINT_NAME = rc.UNIQUE_CONSTRAINT_NAME
                AND pk_kcu.ORDINAL_POSITION = fk_kcu.ORDINAL_POSITION
                WHERE fk_tc.CONSTRAINT_TYPE = 'FOREIGN KEY'
            ) fk
            ON c.TABLE_SCHEMA = fk.TABLE_SCHEMA
            AND c.TABLE_NAME = fk.TABLE_NAME
            AND c.COLUMN_NAME = fk.COLUMN_NAME
            ORDER BY c.TABLE_SCHEMA, c.TABLE_NAME, c.ORDINAL_POSITION;",
                        
                        
                        
                        
                        
            DbProviders.MySql => @"
            SELECT
                c.TABLE_SCHEMA,
                c.TABLE_NAME,
                t.TABLE_TYPE,
                c.COLUMN_NAME,
                c.DATA_TYPE,
                c.IS_NULLABLE,
                c.CHARACTER_MAXIMUM_LENGTH,
                CASE WHEN pk.COLUMN_NAME IS NOT NULL THEN 1 ELSE 0 END AS is_primary_key,
                fk.CONSTRAINT_NAME AS foreign_key_name,
                fk.REFERENCED_TABLE_SCHEMA,
                fk.REFERENCED_TABLE_NAME,
                fk.REFERENCED_COLUMN_NAME
            FROM INFORMATION_SCHEMA.COLUMNS c
            JOIN INFORMATION_SCHEMA.TABLES t
            ON c.TABLE_SCHEMA = t.TABLE_SCHEMA
            AND c.TABLE_NAME = t.TABLE_NAME
            LEFT JOIN (
                SELECT kcu.TABLE_SCHEMA, kcu.TABLE_NAME, kcu.COLUMN_NAME
                FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS tc
                JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                ON tc.CONSTRAINT_NAME = kcu.CONSTRAINT_NAME
                AND tc.TABLE_SCHEMA = kcu.TABLE_SCHEMA
                AND tc.TABLE_NAME = kcu.TABLE_NAME
                WHERE tc.CONSTRAINT_TYPE = 'PRIMARY KEY'
            ) pk
            ON c.TABLE_SCHEMA = pk.TABLE_SCHEMA
            AND c.TABLE_NAME = pk.TABLE_NAME
            AND c.COLUMN_NAME = pk.COLUMN_NAME
            LEFT JOIN (
                SELECT
                    kcu.TABLE_SCHEMA,
                    kcu.TABLE_NAME,
                    kcu.COLUMN_NAME,
                    kcu.CONSTRAINT_NAME,
                    kcu.REFERENCED_TABLE_SCHEMA,
                    kcu.REFERENCED_TABLE_NAME,
                    kcu.REFERENCED_COLUMN_NAME
                FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE kcu
                WHERE kcu.REFERENCED_TABLE_NAME IS NOT NULL
            ) fk
            ON c.TABLE_SCHEMA = fk.TABLE_SCHEMA
            AND c.TABLE_NAME = fk.TABLE_NAME
            AND c.COLUMN_NAME = fk.COLUMN_NAME
            WHERE c.TABLE_SCHEMA = DATABASE()
            ORDER BY c.TABLE_NAME, c.ORDINAL_POSITION;",
            _ => throw new NotSupportedException($"Database Provider {provider} is not supported yet")
        };
    }

    private static string NormalizeTableType(string? rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType))
        {
            return string.Empty;
        }

        return rawType.Equals("BASE TABLE", StringComparison.OrdinalIgnoreCase)
            ? "TABLE"
            : rawType.ToUpperInvariant();
    }

    private static int? ReadNullableInt(IDataRecord reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        if (value is null || value is DBNull)
        {
            return null;
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    private static string? ReadNullableString(IDataRecord reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return null;
        }

        var value = reader.GetValue(ordinal);
        if (value is null || value is DBNull)
        {
            return null;
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture);
    }

    private static bool ReadNullableBool(IDataRecord reader, int ordinal)
    {
        if (reader.IsDBNull(ordinal))
        {
            return false;
        }

        var value = reader.GetValue(ordinal);
        if (value is null || value is DBNull)
        {
            return false;
        }

        if (value is bool boolValue)
        {
            return boolValue;
        }

        if (value is string stringValue)
        {
            return string.Equals(stringValue, "1", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(stringValue, "true", StringComparison.OrdinalIgnoreCase);
        }

        return Convert.ToInt32(value, CultureInfo.InvariantCulture) == 1;
    }

    private sealed class SchemaSnapshotPayload
    {
        public string Database { get; set; } = string.Empty;
        public DateTime CapturedAt { get; set; }
        public List<SchemaTablePayload> Tables { get; set; } = new();
    }

    private sealed class SchemaTablePayload
    {
        public string Schema { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public List<SchemaColumnPayload> Columns { get; set; } = new();
    }

    private sealed class SchemaColumnPayload
    {
        public string Name { get; set; } = string.Empty;
        public string DataType { get; set; } = string.Empty;
        public bool IsPrimaryKey { get; set; }
        public bool IsNullable { get; set; }
        public int? MaxLength { get; set; }
        public string? ForeignKeyName { get; set; }
        public string? ReferencedTableSchema { get; set; }
        public string? ReferencedTableName { get; set; }
        public string? ReferencedColumnName { get; set; }
    }
}
