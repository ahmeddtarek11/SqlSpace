using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Data;
using SqlSpace.Application.Abstractions.Schema;
using SqlSpace.Application.Services.AccessControl;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;

namespace SqlSpace.Application.Services.schema;

public class SchemaContextService(
    IApplicationDbContext context,
    ISchemaExtractor schemaExtractor,
    ILogger<SchemaContextService> logger) : ISchemaContextService
{
    private readonly IApplicationDbContext _context = context;
    private readonly ISchemaExtractor _schemaExtractor = schemaExtractor;
    private readonly ILogger<SchemaContextService> _logger = logger;

    public async Task<string> GetFilteredSchemaForPromptAsync(
        Guid connectionId,
        string userId,
        string? userProvidedSchemaOverride,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(userProvidedSchemaOverride))
        {
            return userProvidedSchemaOverride;
        }

        if (connectionId == Guid.Empty)
        {
            throw new ArgumentException("ConnectionId cannot be empty.", nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("UserId cannot be empty.", nameof(userId));
        }

        var connection = await _context.ConnectedDatabases
            .AsNoTracking()
            .FirstOrDefaultAsync(
                c => c.ConnectionId == connectionId && !c.IsDeleted,
                cancellationToken);

        if (connection is null)
        {
            _logger.LogWarning("Schema context load failed: connection not found. ConnectionId: {ConnectionId}", connectionId);
            throw new InvalidOperationException($"Connection {connectionId} was not found.");
        }

        var isAdmin = string.Equals(connection.DbAdminId, userId, StringComparison.Ordinal);

        UserDatabaseAccess? userAccess = null;
        if (!isAdmin)
        {
            userAccess = await _context.UserDatabaseAccesses
                .AsNoTracking()
                .FirstOrDefaultAsync(
                    a => a.DatabaseConnectionId == connectionId
                        && a.UserId == userId
                        && !a.IsDeleted
                        && a.RevokedAt == null,
                    cancellationToken);

            if (userAccess is null)
            {
                _logger.LogWarning(
                    "Schema context load denied: user has no access. ConnectionId: {ConnectionId}, UserId: {UserId}",
                    connectionId,
                    userId);
                return string.Empty;
            }
        }

        var snapshot = await LoadLatestSnapshotAsync(connectionId, cancellationToken);
        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.SchemaText))
        {
            await RefreshSchemaAsync(connectionId, userId, cancellationToken);
            snapshot = await LoadLatestSnapshotAsync(connectionId, cancellationToken);
        }

        if (snapshot is null || string.IsNullOrWhiteSpace(snapshot.SchemaText))
        {
            _logger.LogWarning(
                "Schema context load failed: no schema snapshot available after refresh. ConnectionId: {ConnectionId}",
                connectionId);
            return string.Empty;
        }

        if (isAdmin || userAccess?.HasFullAccess == true)
        {
            return snapshot.SchemaText;
        }

        if (string.IsNullOrWhiteSpace(userAccess?.RestrictedTablesJson))
        {
            _logger.LogWarning(
                "Schema context load denied: user has restricted access but no restriction list. ConnectionId: {ConnectionId}, UserId: {UserId}",
                connectionId,
                userId);
            return SerializeSchema(snapshot.SchemaText, null, connection.DatabaseProvider, denyAll: true);
        }

        IReadOnlyList<TableRestrictionDto> restrictedTables;
        try
        {
            restrictedTables = userAccess.RestrictedTablesJson
                .GetRestrictedTables(connection.DatabaseProvider);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Schema context load failed: invalid RestrictedTablesJson. ConnectionId: {ConnectionId}, UserId: {UserId}",
                connectionId,
                userId);
            return string.Empty;
        }

        var restrictedKeys = restrictedTables
            .Select(rt => connection.DatabaseProvider.BuildTableKey(rt.Table, rt.Schema))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return SerializeSchema(snapshot.SchemaText, restrictedKeys, connection.DatabaseProvider);
    }

    public async Task RefreshSchemaAsync(Guid connectionId, string requestedByUserId, CancellationToken cancellationToken)
    {
        if (connectionId == Guid.Empty)
        {
            throw new ArgumentException("ConnectionId cannot be empty.", nameof(connectionId));
        }

        if (string.IsNullOrWhiteSpace(requestedByUserId))
        {
            throw new ArgumentException("RequestedByUserId cannot be empty.", nameof(requestedByUserId));
        }

        var connection = await _context.ConnectedDatabases
            .FirstOrDefaultAsync(
                c => c.ConnectionId == connectionId && !c.IsDeleted,
                cancellationToken);

        if (connection is null)
        {
            _logger.LogWarning("Schema refresh failed: connection not found. ConnectionId: {ConnectionId}", connectionId);
            throw new InvalidOperationException($"Connection {connectionId} was not found.");
        }

        var schemaJson = await _schemaExtractor.ExtractSchemaJsonAsync(connection, cancellationToken);
        if (string.IsNullOrWhiteSpace(schemaJson))
        {
            _logger.LogWarning(
                "Schema refresh failed: extractor returned empty schema. ConnectionId: {ConnectionId}",
                connectionId);
            return;
        }

        var schemaHash = ComputeSchemaHash(schemaJson);

        var latestSnapshot = await _context.DatabaseSchemaSnapshots
            .FirstOrDefaultAsync(
                s => s.DatabaseConnectionId == connectionId && s.IsLatest,
                cancellationToken);

        if (latestSnapshot is not null &&
            string.Equals(latestSnapshot.SchemaHash, schemaHash, StringComparison.Ordinal))
        {
            return;
        }

        await using var transaction = await _context.BeginTransactionAsync(cancellationToken);
        try
        {
            if (latestSnapshot is not null)
            {
                latestSnapshot.IsLatest = false;
            }

            var extraLatestSnapshots = await _context.DatabaseSchemaSnapshots
                .Where(s => s.DatabaseConnectionId == connectionId && s.IsLatest)
                .ToListAsync(cancellationToken);

            foreach (var snapshot in extraLatestSnapshots)
            {
                snapshot.IsLatest = false;
            }

            var newSnapshot = new DatabaseSchemaSnapshot
            {
                SnapshotId = Guid.NewGuid(),
                DatabaseConnectionId = connectionId,
                SchemaText = schemaJson,
                IsLatest = true,
                CapturedAt = DateTime.UtcNow,
                SchemaHash = schemaHash
            };

            await _context.DatabaseSchemaSnapshots.AddAsync(newSnapshot, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(
                ex,
                "Schema refresh failed while saving snapshots. ConnectionId: {ConnectionId}",
                connectionId);
            throw;
        }
    }

    private async Task<DatabaseSchemaSnapshot?> LoadLatestSnapshotAsync(Guid connectionId, CancellationToken cancellationToken)
    {
        return await _context.DatabaseSchemaSnapshots
            .AsNoTracking()
            .FirstOrDefaultAsync(
                s => s.DatabaseConnectionId == connectionId && s.IsLatest,
                cancellationToken);
    }

    private static string SerializeSchema(
        string schemaJson,
        IEnumerable<string>? restrictedKeys,
        DbProviders provider,
        bool denyAll = false)
    {
        SchemaSnapshotPayload? snapshot;
        try
        {
            snapshot = JsonSerializer.Deserialize<SchemaSnapshotPayload>(schemaJson, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return string.Empty;
        }

        if (snapshot is null)
        {
            return string.Empty;
        }

        if (denyAll)
        {
            snapshot.Tables = new List<SchemaTablePayload>();
        }
        else if (restrictedKeys is not null)
        {
            var restrictedSet = restrictedKeys.ToHashSet(StringComparer.OrdinalIgnoreCase);

            snapshot.Tables = snapshot.Tables
                .Where(t => !restrictedSet.Contains(provider.BuildTableKey(t.Name, t.Schema)))
                .ToList();
        }

        return JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        });
    }

    private static string ComputeSchemaHash(string schemaJson)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(schemaJson);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToHexString(hashBytes);
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
