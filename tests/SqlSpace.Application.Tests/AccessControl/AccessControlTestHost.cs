using System.Text.Json;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSpace.Application.Abstractions.Users.Dtos;
using SqlSpace.Application.Services.AccessControl;
using SqlSpace.Application.Tests.AccessControl.Fakes;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;
using SqlSpace.Infrastructure.Data;
using SqlSpace.Infrastructure.Identity;

namespace SqlSpace.Application.Tests.AccessControl;

public static class AccessControlTestHost
{
    public static readonly Guid ConnectionId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    public static readonly Guid SecondaryConnectionId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    public const string AdminId = "admin-1";
    public const string AdminEmail = "admin@sqlspace.dev";
    public const string AdminUserName = "admin";

    public const string UserId = "user-1";
    public const string UserEmail = "user@sqlspace.dev";
    public const string UserUserName = "user";

    public const string TargetId = "target-1";
    public const string TargetEmail = "target@sqlspace.dev";
    public const string TargetUserName = "target";

    public static async Task<(ApplicationDbContext Context, SqliteConnection Connection)> CreateDbContextAsync()
    {
        var connection = new SqliteConnection("Filename=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ApplicationDbContext(options);
        await context.Database.EnsureCreatedAsync();

        return (context, connection);
    }

    public static AccessControlService CreateService(
        ApplicationDbContext context,
        FakeUserRepository userRepository,
        FakeAuditLogRepository auditRepository)
    {
        return new AccessControlService(
            context,
            userRepository,
            NullLogger<AccessControlService>.Instance,
            auditRepository);
    }

    public static userDto NewUser(string id, string email, string userName)
    {
        return new userDto
        {
            Id = id,
            Email = email,
            UserName = userName
        };
    }

    public static async Task SeedIdentityUserAsync(
        ApplicationDbContext context,
        string id,
        string email,
        string userName)
    {
        if (await context.Users.AnyAsync(user => user.Id == id))
        {
            return;
        }

        context.Users.Add(new ApplicationUser
        {
            Id = id,
            Email = email,
            NormalizedEmail = email.ToUpperInvariant(),
            UserName = userName,
            NormalizedUserName = userName.ToUpperInvariant(),
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N")
        });

        await context.SaveChangesAsync();
    }

    public static async Task<ConnectedDatabase> SeedConnectionAsync(
        ApplicationDbContext context,
        Guid connectionId,
        string adminUserId,
        DbProviders provider = DbProviders.PostgreSql)
    {
        var existing = await context.ConnectedDatabases
            .FirstOrDefaultAsync(db => db.ConnectionId == connectionId);
        if (existing is not null)
        {
            return existing;
        }

        var connection = new ConnectedDatabase
        {
            ConnectionId = connectionId,
            CreatedByUserId = adminUserId,
            DbAdminId = adminUserId,
            ConnectionName = $"conn-{connectionId:N}",
            DatabaseName = "db_main",
            DatabaseProvider = provider,
            Host = "localhost",
            PortNumber = provider.GetDefaultPort(),
            UseSSL = false,
            UsesRawConnectionString = false,
            LastSuccessfulConnection = DateTime.UtcNow,
            IsHealthy = true,
            CreatedAt = DateTime.UtcNow,
            IsDeleted = false
        };

        context.ConnectedDatabases.Add(connection);
        await context.SaveChangesAsync();
        return connection;
    }

    public static async Task<UserDatabaseAccess> SeedUserAccessAsync(
        ApplicationDbContext context,
        Guid connectionId,
        string userId,
        string grantedByUserId,
        bool hasFullAccess,
        string? restrictedTablesJson = null)
    {
        var access = new UserDatabaseAccess
        {
            Id = Guid.NewGuid(),
            DatabaseConnectionId = connectionId,
            UserId = userId,
            GrantedByUserId = grantedByUserId,
            GrantedAt = DateTime.UtcNow,
            HasFullAccess = hasFullAccess,
            RestrictedTablesJson = restrictedTablesJson,
            IsDeleted = false,
            RevokedAt = null,
            RevokedByUserId = null
        };

        context.UserDatabaseAccesses.Add(access);
        await context.SaveChangesAsync();
        return access;
    }

    public static async Task<DatabaseSchemaSnapshot> SeedSchemaSnapshotAsync(
        ApplicationDbContext context,
        Guid connectionId,
        params (string schema, string table)[] tables)
    {
        var schemaText = JsonSerializer.Serialize(new
        {
            Database = "db_main",
            Tables = tables.Select(table => new
            {
                Schema = table.schema,
                Name = table.table
            }).ToList()
        });

        var snapshot = new DatabaseSchemaSnapshot
        {
            SnapshotId = Guid.NewGuid(),
            DatabaseConnectionId = connectionId,
            IsLatest = true,
            CapturedAt = DateTime.UtcNow,
            SchemaHash = Guid.NewGuid().ToString("N"),
            SchemaText = schemaText
        };

        context.DatabaseSchemaSnapshots.Add(snapshot);
        await context.SaveChangesAsync();
        return snapshot;
    }

    public static string JsonRestrictions(params (string schema, string table)[] rows)
    {
        var payload = rows.Select(row => new RestrictionRow
        {
            Schema = row.schema,
            Table = row.table
        });

        return JsonSerializer.Serialize(payload);
    }

    private sealed class RestrictionRow
    {
        public string Schema { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
    }
}
