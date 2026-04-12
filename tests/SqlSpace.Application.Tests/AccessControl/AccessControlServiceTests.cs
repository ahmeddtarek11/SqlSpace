using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SqlSpace.Application.Abstractions.Access;
using SqlSpace.Application.Services.AccessControl;
using SqlSpace.Application.Tests.AccessControl.Fakes;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;
using SqlSpace.Infrastructure.Data;

namespace SqlSpace.Application.Tests.AccessControl;

public sealed class AccessControlServiceTests
{
    [Fact]
    public async Task CanAccessTableAsync_ShouldReturnInvalidConnectionId_WhenConnectionIdIsEmpty()
    {
        await using var harness = await TestHarness.CreateAsync();

        var result = await harness.Service.CanAccessTableAsync(
            Guid.Empty,
            AccessControlTestHost.UserId,
            "orders",
            "public",
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(AccessControlErrors.InvalidConnectionIdCode);
    }

    [Fact]
    public async Task CanAccessTableAsync_ShouldReturnTrue_ForConnectionAdmin()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);

        var result = await harness.Service.CanAccessTableAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            "orders",
            "public",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task CanAccessTableAsync_ShouldReturnFalse_WhenTableIsRestricted()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName);

        await AccessControlTestHost.SeedUserAccessAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            AccessControlTestHost.AdminId,
            hasFullAccess: false,
            restrictedTablesJson: AccessControlTestHost.JsonRestrictions(("public", "orders")));

        var result = await harness.Service.CanAccessTableAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            "orders",
            "public",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task CanAccessTableAsync_ShouldReturnFalse_WhenRestrictedTablesJsonIsMalformed()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName);

        await AccessControlTestHost.SeedUserAccessAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            AccessControlTestHost.AdminId,
            hasFullAccess: false,
            restrictedTablesJson: "{bad json");

        var result = await harness.Service.CanAccessTableAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            "orders",
            "public",
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
    }

    [Fact]
    public async Task GetAccessibleTableNamesAsync_ShouldReturnAllTables_ForAdmin()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedSchemaSnapshotAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            ("public", "orders"),
            ("public", "customers"));

        var result = await harness.Service.GetAccessibleTableNamesAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(["public.orders", "public.customers"]);
    }

    [Fact]
    public async Task GetAccessibleTableNamesAsync_ShouldFilterRestrictedTables_ForRestrictedUser()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName);

        await AccessControlTestHost.SeedUserAccessAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            AccessControlTestHost.AdminId,
            hasFullAccess: false,
            restrictedTablesJson: AccessControlTestHost.JsonRestrictions(("public", "orders")));

        await AccessControlTestHost.SeedSchemaSnapshotAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            ("public", "orders"),
            ("public", "customers"));

        var result = await harness.Service.GetAccessibleTableNamesAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEquivalentTo(["public.customers"]);
    }

    [Fact]
    public async Task GetAccessibleTableNamesAsync_ShouldReturnEmpty_WhenNoUserAccessExists()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName);

        var result = await harness.Service.GetAccessibleTableNamesAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAccessibleTableNamesAsync_ShouldReturnSchemaSnapshotNotFound_WhenSnapshotMissing()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName);

        await AccessControlTestHost.SeedUserAccessAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            AccessControlTestHost.AdminId,
            hasFullAccess: false,
            restrictedTablesJson: AccessControlTestHost.JsonRestrictions(("public", "orders")));

        var result = await harness.Service.GetAccessibleTableNamesAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(AccessControlErrors.SchemaSnapshotNotFoundCode);
    }

    [Fact]
    public async Task HasAccessToConnectionAsync_ShouldReturnTrue_ForAdmin()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);

        var result = await harness.Service.HasAccessToConnectionAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task HasAccessToConnectionAsync_ShouldReturnTrue_ForUserWithActiveAccess()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName);

        await AccessControlTestHost.SeedUserAccessAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            AccessControlTestHost.AdminId,
            hasFullAccess: true);

        var result = await harness.Service.HasAccessToConnectionAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();
    }

    [Fact]
    public async Task ListConnectionUsersAsync_ShouldReturnMappedSummaries_WithCanonicalRestrictedTables()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.MySql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName);

        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.AdminId,
            AccessControlTestHost.AdminEmail,
            AccessControlTestHost.AdminUserName));
        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName));

        await AccessControlTestHost.SeedUserAccessAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            AccessControlTestHost.AdminId,
            hasFullAccess: false,
            restrictedTablesJson: AccessControlTestHost.JsonRestrictions(
                ("analytics", "orders"),
                ("", "orders"),
                ("ignored", "customers")));

        var result = await harness.Service.ListConnectionUsersAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);

        var summary = result.Value!.Single();
        summary.UserEmail.Should().Be(AccessControlTestHost.UserEmail);
        summary.GrantedByUserEmail.Should().Be(AccessControlTestHost.AdminEmail);
        summary.RestrictedTables.Should().BeEquivalentTo(["orders", "customers"]);
    }

    [Fact]
    public async Task ListConnectionUsersAsync_ShouldHandleMalformedRestrictionJson_AndReturnEmptyRestrictionsForRow()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName);

        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.AdminId,
            AccessControlTestHost.AdminEmail,
            AccessControlTestHost.AdminUserName));
        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.UserId,
            AccessControlTestHost.UserEmail,
            AccessControlTestHost.UserUserName));

        await AccessControlTestHost.SeedUserAccessAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.UserId,
            AccessControlTestHost.AdminId,
            hasFullAccess: false,
            restrictedTablesJson: "{ malformed json");

        var result = await harness.Service.ListConnectionUsersAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(1);
        result.Value!.Single().RestrictedTables.Should().BeEmpty();
    }

    [Fact]
    public async Task RevokeAccessAsync_ShouldSoftDeleteActiveAccess_AndReturnTrue()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.TargetId,
            AccessControlTestHost.TargetEmail,
            AccessControlTestHost.TargetUserName);

        var access = await AccessControlTestHost.SeedUserAccessAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.TargetId,
            AccessControlTestHost.AdminId,
            hasFullAccess: true);

        var result = await harness.Service.RevokeAccessAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            AccessControlTestHost.TargetId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        var persisted = await harness.Context.UserDatabaseAccesses
            .FirstAsync(a => a.Id == access.Id);

        persisted.IsDeleted.Should().BeTrue();
        persisted.RevokedByUserId.Should().Be(AccessControlTestHost.AdminId);
        persisted.RevokedAt.Should().NotBeNull();
        harness.AuditRepository.RevokeCalls.Should().Be(1);
    }

    [Fact]
    public async Task RevokeAccessAsync_ShouldReturnFalse_WhenNoActiveAccessExists()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.TargetId,
            AccessControlTestHost.TargetEmail,
            AccessControlTestHost.TargetUserName);

        var result = await harness.Service.RevokeAccessAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            AccessControlTestHost.TargetId,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeFalse();
        harness.AuditRepository.RevokeCalls.Should().Be(0);
    }

    [Fact]
    public async Task GrantAccessAsync_ShouldPersistFullAccess_AndWriteAudit()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.TargetId,
            AccessControlTestHost.TargetEmail,
            AccessControlTestHost.TargetUserName);

        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.AdminId,
            AccessControlTestHost.AdminEmail,
            AccessControlTestHost.AdminUserName));
        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.TargetId,
            AccessControlTestHost.TargetEmail,
            AccessControlTestHost.TargetUserName));

        var result = await harness.Service.GrantAccessAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            AccessControlTestHost.TargetEmail,
            hasFullAccess: true,
            restrictedTables: null,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();

        var persisted = await harness.Context.UserDatabaseAccesses
            .FirstAsync(a => a.DatabaseConnectionId == AccessControlTestHost.ConnectionId
                && a.UserId == AccessControlTestHost.TargetId
                && !a.IsDeleted);

        persisted.HasFullAccess.Should().BeTrue();
        persisted.RestrictedTablesJson.Should().BeNull();
        harness.AuditRepository.GrantCalls.Should().Be(1);
        harness.AuditRepository.LastGrantHasFullAccess.Should().BeTrue();
    }

    [Fact]
    public async Task GrantAccessAsync_ShouldSucceedForMySqlRestrictedAccess_WithEmptySchema()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.MySql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.TargetId,
            AccessControlTestHost.TargetEmail,
            AccessControlTestHost.TargetUserName);

        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.AdminId,
            AccessControlTestHost.AdminEmail,
            AccessControlTestHost.AdminUserName));
        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.TargetId,
            AccessControlTestHost.TargetEmail,
            AccessControlTestHost.TargetUserName));

        var restrictions = new List<TableRestrictionInput>
        {
            new() { Schema = string.Empty, Table = "orders" }
        };

        var result = await harness.Service.GrantAccessAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            AccessControlTestHost.TargetEmail,
            hasFullAccess: false,
            restrictedTables: restrictions,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.RestrictedTables.Should().BeEquivalentTo(["orders"]);

        var persisted = await harness.Context.UserDatabaseAccesses
            .FirstAsync(a => a.DatabaseConnectionId == AccessControlTestHost.ConnectionId
                && a.UserId == AccessControlTestHost.TargetId
                && !a.IsDeleted);

        persisted.HasFullAccess.Should().BeFalse();
        var stored = DeserializeRestrictions(persisted.RestrictedTablesJson);
        stored.Should().HaveCount(1);
        stored[0].Schema.Should().BeEmpty();
        stored[0].Table.Should().Be("orders");
    }

    [Fact]
    public async Task GrantAccessAsync_ShouldRollback_WhenAuditLoggingFails()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.PostgreSql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.TargetId,
            AccessControlTestHost.TargetEmail,
            AccessControlTestHost.TargetUserName);

        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.AdminId,
            AccessControlTestHost.AdminEmail,
            AccessControlTestHost.AdminUserName));
        harness.UserRepository.AddUser(AccessControlTestHost.NewUser(
            AccessControlTestHost.TargetId,
            AccessControlTestHost.TargetEmail,
            AccessControlTestHost.TargetUserName));

        harness.AuditRepository.FailGrantLog = true;

        var result = await harness.Service.GrantAccessAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            AccessControlTestHost.TargetEmail,
            hasFullAccess: true,
            restrictedTables: null,
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(AccessControlErrors.GrantAccessFailedCode);

        var exists = await harness.Context.UserDatabaseAccesses.AnyAsync(a =>
            a.DatabaseConnectionId == AccessControlTestHost.ConnectionId &&
            a.UserId == AccessControlTestHost.TargetId &&
            !a.IsDeleted);

        exists.Should().BeFalse();
        harness.AuditRepository.GrantCalls.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAccessRestrictionsAsync_ShouldNormalizeAndDeduplicateForMySql_AndSucceedWhenAuditFails()
    {
        await using var harness = await TestHarness.CreateAsync();
        await SeedAdminAndConnectionAsync(harness, DbProviders.MySql);
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.TargetId,
            AccessControlTestHost.TargetEmail,
            AccessControlTestHost.TargetUserName);

        var access = await AccessControlTestHost.SeedUserAccessAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.TargetId,
            AccessControlTestHost.AdminId,
            hasFullAccess: true,
            restrictedTablesJson: null);

        harness.AuditRepository.FailUpdateRestrictionsLog = true;

        var restrictions = new List<TableRestrictionInput>
        {
            new() { Schema = "dbo", Table = "orders" },
            new() { Schema = string.Empty, Table = "orders" },
            new() { Schema = "ignored", Table = "customers" }
        };

        var result = await harness.Service.UpdateAccessRestrictionsAsync(
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            AccessControlTestHost.TargetId,
            hasFullAccess: false,
            restrictedTables: restrictions,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        var persisted = await harness.Context.UserDatabaseAccesses
            .FirstAsync(a => a.Id == access.Id);

        persisted.HasFullAccess.Should().BeFalse();
        var stored = DeserializeRestrictions(persisted.RestrictedTablesJson);
        stored.Select(s => s.Table).Should().BeEquivalentTo(["orders", "customers"]);
        stored.All(s => string.IsNullOrEmpty(s.Schema)).Should().BeTrue();
        harness.AuditRepository.UpdateCalls.Should().Be(1);
    }

    private static async Task SeedAdminAndConnectionAsync(TestHarness harness, DbProviders provider)
    {
        await AccessControlTestHost.SeedIdentityUserAsync(
            harness.Context,
            AccessControlTestHost.AdminId,
            AccessControlTestHost.AdminEmail,
            AccessControlTestHost.AdminUserName);

        await AccessControlTestHost.SeedConnectionAsync(
            harness.Context,
            AccessControlTestHost.ConnectionId,
            AccessControlTestHost.AdminId,
            provider);
    }

    private static List<StoredRestriction> DeserializeRestrictions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<StoredRestriction>();
        }

        return JsonSerializer.Deserialize<List<StoredRestriction>>(json)
            ?? new List<StoredRestriction>();
    }

    private sealed class StoredRestriction
    {
        public string Schema { get; set; } = string.Empty;
        public string Table { get; set; } = string.Empty;
    }

    private sealed class TestHarness : IAsyncDisposable
    {
        private TestHarness(
            ApplicationDbContext context,
            SqliteConnection connection,
            FakeUserRepository userRepository,
            FakeAuditLogRepository auditRepository,
            AccessControlService service)
        {
            Context = context;
            Connection = connection;
            UserRepository = userRepository;
            AuditRepository = auditRepository;
            Service = service;
        }

        public ApplicationDbContext Context { get; }
        public SqliteConnection Connection { get; }
        public FakeUserRepository UserRepository { get; }
        public FakeAuditLogRepository AuditRepository { get; }
        public AccessControlService Service { get; }

        public static async Task<TestHarness> CreateAsync()
        {
            var (context, connection) = await AccessControlTestHost.CreateDbContextAsync();
            var userRepository = new FakeUserRepository();
            var auditRepository = new FakeAuditLogRepository();
            var service = AccessControlTestHost.CreateService(context, userRepository, auditRepository);

            return new TestHarness(context, connection, userRepository, auditRepository, service);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await Connection.DisposeAsync();
        }
    }
}
