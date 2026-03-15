using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSpace.Application.Abstractions.Audit;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.Abstractions.Security;
using SqlSpace.Application.Abstractions.Users;
using SqlSpace.Application.Abstractions.Users.Dtos;
using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Application.DTOs.Query;
using SqlSpace.Application.Services.Connection;
using SqlSpace.Domain.Common.Errors;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;
using SqlSpace.Infrastructure.Data;
using SqlSpace.Infrastructure.Identity;

namespace SqlSpace.Application.Tests.Connection;

public sealed class ConnectionManagementServiceTests
{
    [Fact]
    public async Task GetUserConnectionsAsync_ShouldReturnUserIdRequired_WhenUserIdIsEmpty()
    {
        await using var harness = await TestHarness.CreateAsync();

        var result = await harness.Service.GetUserConnectionsAsync(" ", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(ConnectionErrors.UserIdIsRequiredCode);
    }

    [Fact]
    public async Task GetUserConnectionsAsync_ShouldReturnInvalidUserId_WhenUserDoesNotExist()
    {
        await using var harness = await TestHarness.CreateAsync();

        var result = await harness.Service.GetUserConnectionsAsync("missing-user", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(ConnectionErrors.InvalidRequestCode);
    }

    [Fact]
    public async Task GetUserConnectionsAsync_ShouldReturnOwnedAndGrantedConnections_OrderedByCreatedAtDesc()
    {
        await using var harness = await TestHarness.CreateAsync();
        const string userId = "user-1";
        const string admin2Id = "admin-2";

        await harness.SeedUserAsync(userId, "user@sqlspace.dev", "user");
        await harness.SeedUserAsync(admin2Id, "admin2@sqlspace.dev", "admin2");

        var now = DateTime.UtcNow;
        var ownedOld = await harness.SeedConnectionAsync(Guid.NewGuid(), userId, createdAt: now.AddMinutes(-90));
        var ownedNew = await harness.SeedConnectionAsync(Guid.NewGuid(), userId, createdAt: now.AddMinutes(-30));
        var granted = await harness.SeedConnectionAsync(Guid.NewGuid(), admin2Id, createdAt: now.AddMinutes(-10));
        var revoked = await harness.SeedConnectionAsync(Guid.NewGuid(), admin2Id, createdAt: now.AddMinutes(-5));
        await harness.SeedConnectionAsync(Guid.NewGuid(), userId, createdAt: now.AddMinutes(-1), isDeleted: true);

        await harness.SeedUserAccessAsync(granted.ConnectionId, userId, admin2Id, hasFullAccess: false);
        await harness.SeedUserAccessAsync(revoked.ConnectionId, userId, admin2Id, hasFullAccess: true, revokedAt: DateTime.UtcNow);

        var result = await harness.Service.GetUserConnectionsAsync(userId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Should().HaveCount(3);
        result!.Value!.Select(c => c.ConnectionId).Should().ContainInOrder(
            granted.ConnectionId,
            ownedNew.ConnectionId,
            ownedOld.ConnectionId);

        result!.Value!.First(c => c.ConnectionId == granted.ConnectionId).IsAdmin.Should().BeFalse();
        result!.Value!.First(c => c.ConnectionId == granted.ConnectionId).HasFullAccess.Should().BeFalse();
        result!.Value!.First(c => c.ConnectionId == ownedNew.ConnectionId).IsAdmin.Should().BeTrue();
        result!.Value!.First(c => c.ConnectionId == ownedNew.ConnectionId).HasFullAccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateConnectionAsync_ShouldReturnInvalidRequest_WhenRequestIsNull()
    {
        await using var harness = await TestHarness.CreateAsync();

        var result = await harness.Service.CreateConnectionAsync("user-1", null!, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(ConnectionErrors.InvalidRequestCode);
    }

    [Fact]
    public async Task CreateConnectionAsync_ShouldCreateRawConnection_WhenTestSucceeds()
    {
        await using var harness = await TestHarness.CreateAsync();
        const string userId = "creator-1";
        await harness.SeedUserAsync(userId, "creator@sqlspace.dev", "creator");

        harness.ConnectionStringBuilder.ValidationResult = new ConnectionStringValidationResult
        {
            IsValid = true,
            ParsedComponents = new ConnectionComponents
            {
                Host = "db.local",
                Port = 5432,
                DatabaseName = "sales",
                Username = "db-user",
                UseSSL = true,
                AdditionalParameters = "Pooling=true"
            }
        };
        harness.DatabaseExecutor.NextTestConnectionResult = new ConnectionTestResult
        {
            Success = true,
            DatabaseName = "sales",
            ResponseTimeMs = 8
        };

        var request = new CreateConnectionRequest
        {
            ConnectionName = "prod-main",
            DatabaseProvider = DbProviders.PostgreSql,
            InputMode = ConnectionInputMode.RawConnectionString,
            RawConnectionString = "  Host=db.local;Port=5432;Database=sales;Username=db-user;Password=topsecret  ",
            UseSSL = true
        };

        var result = await harness.Service.CreateConnectionAsync(userId, request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        var persisted = await harness.Context.ConnectedDatabases.SingleAsync(c => c.ConnectionName == "prod-main");
        persisted.DbAdminId.Should().Be(userId);
        persisted.UsesRawConnectionString.Should().BeTrue();
        persisted.EncryptedRawConnectionString.Should().Be("enc::Host=db.local;Port=5432;Database=sales;Username=db-user;Password=topsecret");
    }

    [Fact]
    public async Task DeleteConnectionAsync_ShouldSoftDeleteConnectionAndActiveAccesses()
    {
        await using var harness = await TestHarness.CreateAsync();
        const string adminId = "admin-1";
        const string userId = "user-1";
        await harness.SeedUserAsync(adminId, "admin@sqlspace.dev", "admin");
        await harness.SeedUserAsync(userId, "user@sqlspace.dev", "user");
        var connection = await harness.SeedConnectionAsync(Guid.NewGuid(), adminId);
        await harness.SeedUserAccessAsync(connection.ConnectionId, userId, adminId, hasFullAccess: true);

        var result = await harness.Service.DeleteConnectionAsync(connection.ConnectionId, adminId, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        var persistedConnection = await harness.Context.ConnectedDatabases.SingleAsync(c => c.ConnectionId == connection.ConnectionId);
        persistedConnection.IsDeleted.Should().BeTrue();
        persistedConnection.DeletedByUserId.Should().Be(adminId);

        var persistedAccess = await harness.Context.UserDatabaseAccesses.SingleAsync(a => a.DatabaseConnectionId == connection.ConnectionId);
        persistedAccess.IsDeleted.Should().BeTrue();
        persistedAccess.RevokedByUserId.Should().Be(adminId);
        persistedAccess.RevokedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetConnectionByIdAsync_ShouldReturnForbiddenError_WhenUserHasNoAccess()
    {
        await using var harness = await TestHarness.CreateAsync();
        const string adminId = "admin-1";
        const string userId = "user-1";
        await harness.SeedUserAsync(adminId, "admin@sqlspace.dev", "admin");
        await harness.SeedUserAsync(userId, "user@sqlspace.dev", "user");
        var connection = await harness.SeedConnectionAsync(Guid.NewGuid(), adminId);

        var result = await harness.Service.GetConnectionByIdAsync(connection.ConnectionId, userId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(ConnectionErrors.AdminNotOwnerCode);
    }

    [Fact]
    public async Task UpdatePasswordAsync_ShouldReturnInvalidConnectionString_WhenRawConnectionStringMissing()
    {
        await using var harness = await TestHarness.CreateAsync();
        const string adminId = "admin-1";
        await harness.SeedUserAsync(adminId, "admin@sqlspace.dev", "admin");
        var connection = await harness.SeedConnectionAsync(Guid.NewGuid(), adminId);
        connection.UsesRawConnectionString = true;
        connection.EncryptedRawConnectionString = null;
        await harness.Context.SaveChangesAsync();

        var result = await harness.Service.UpdatePasswordAsync(connection.ConnectionId, adminId, "newPassword", CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(ConnectionErrors.InvalidConnectionStringCode);
    }

    [Fact]
    public async Task UpdatePasswordAsync_ShouldUpdateEncryptedPasswordAndRawConnection_WhenComponentModeSucceeds()
    {
        await using var harness = await TestHarness.CreateAsync();
        const string adminId = "admin-1";
        await harness.SeedUserAsync(adminId, "admin@sqlspace.dev", "admin");
        var connection = await harness.SeedConnectionAsync(Guid.NewGuid(), adminId);
        connection.UsesRawConnectionString = false;
        connection.EncryptedPassword = "enc::old-password";
        connection.Host = "server.local";
        connection.DatabaseName = "main_db";
        connection.Username = "db-user";
        connection.PortNumber = 5432;
        await harness.Context.SaveChangesAsync();

        harness.ConnectionStringBuilder.BuildResult = "Host=server.local;Port=5432;Database=main_db;Username=db-user;Password=new-password";
        harness.DatabaseExecutor.NextTestConnectionResult = new ConnectionTestResult
        {
            Success = true,
            ResponseTimeMs = 12
        };

        var result = await harness.Service.UpdatePasswordAsync(connection.ConnectionId, adminId, "  new-password  ", CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        var persisted = await harness.Context.ConnectedDatabases.SingleAsync(c => c.ConnectionId == connection.ConnectionId);
        persisted.EncryptedPassword.Should().Be("enc::new-password");
        persisted.EncryptedRawConnectionString.Should().Be("enc::Host=server.local;Port=5432;Database=main_db;Username=db-user;Password=new-password");
        persisted.IsHealthy.Should().BeTrue();
        harness.DatabaseExecutor.TestConnectionCalls.Should().Be(1);
        harness.ConnectionStringBuilder.LastBuildPassword.Should().Be("new-password");
    }

    [Fact]
    public async Task TestNewConnectionAsync_ShouldReturnHostRequired_ForInvalidIndividualFieldsRequest()
    {
        await using var harness = await TestHarness.CreateAsync();

        var request = new TestConnectionRequest
        {
            InputMode = ConnectionInputMode.IndividualFields,
            DatabaseProvider = DbProviders.PostgreSql,
            Host = " ",
            DatabaseName = "sales",
            Username = "db-user",
            Password = "secret"
        };

        var result = await harness.Service.TestNewConnectionAsync(request, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(ConnectionErrors.HostRequiredCode);
    }

    [Fact]
    public async Task TestNewConnectionAsync_ShouldSucceed_ForRawModeAndPopulateExtractedComponents()
    {
        await using var harness = await TestHarness.CreateAsync();
        harness.ConnectionStringBuilder.ValidationResult = new ConnectionStringValidationResult
        {
            IsValid = true,
            ParsedComponents = new ConnectionComponents
            {
                Host = "db.local",
                Port = 5432,
                DatabaseName = "sales",
                Username = "db-user",
                UseSSL = true,
                AdditionalParameters = "Pooling=true"
            }
        };
        harness.DatabaseExecutor.NextTestConnectionResult = new ConnectionTestResult
        {
            Success = true,
            DatabaseName = "sales",
            ResponseTimeMs = 9
        };

        var request = new TestConnectionRequest
        {
            InputMode = ConnectionInputMode.RawConnectionString,
            DatabaseProvider = DbProviders.PostgreSql,
            RawConnectionString = "  Host=db.local;Port=5432;Database=sales;Username=db-user;Password=secret  "
        };

        var result = await harness.Service.TestNewConnectionAsync(request, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.ExtractedComponents.Should().NotBeNull();
        result.Value.ExtractedComponents!.Host.Should().Be("db.local");
        result.Value.ExtractedComponents.Port.Should().Be(5432);
        result.Value.ExtractedComponents.Password.Should().BeEmpty();
        harness.EncryptionService.EncryptInputs.Should().ContainSingle()
            .Which.Should().Be("Host=db.local;Port=5432;Database=sales;Username=db-user;Password=secret");
    }

    [Fact]
    public async Task TestExistingConnectionHealthAsync_ShouldMarkConnectionUnhealthy_WhenTestFails()
    {
        await using var harness = await TestHarness.CreateAsync();
        const string adminId = "admin-1";
        await harness.SeedUserAsync(adminId, "admin@sqlspace.dev", "admin");
        var connection = await harness.SeedConnectionAsync(Guid.NewGuid(), adminId);
        connection.IsHealthy = true;
        await harness.Context.SaveChangesAsync();

        harness.DatabaseExecutor.NextTestConnectionResult = new ConnectionTestResult
        {
            Success = false,
            ErrorMessage = "timeout"
        };

        var result = await harness.Service.TestExistingConnectionHealthAsync(connection.ConnectionId, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Errors[0].Code.Should().Be(ConnectionErrors.ConnectionTestFailedCode);

        var persisted = await harness.Context.ConnectedDatabases.SingleAsync(c => c.ConnectionId == connection.ConnectionId);
        persisted.IsHealthy.Should().BeFalse();
    }

    [Fact]
    public async Task TransferOwnershipAsync_ShouldTransferOwnerAndAdjustAccessRows()
    {
        await using var harness = await TestHarness.CreateAsync();
        const string oldAdminId = "admin-old";
        const string newAdminId = "admin-new";
        const string oldEmail = "old@sqlspace.dev";
        const string newEmail = "new@sqlspace.dev";

        await harness.SeedUserAsync(oldAdminId, oldEmail, "old");
        await harness.SeedUserAsync(newAdminId, newEmail, "new");
        var connection = await harness.SeedConnectionAsync(Guid.NewGuid(), oldAdminId);
        await harness.SeedUserAccessAsync(connection.ConnectionId, newAdminId, oldAdminId, hasFullAccess: true);

        var result = await harness.Service.TransferOwnershipAsync(
            connection.ConnectionId,
            oldAdminId,
            newEmail,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        var persistedConnection = await harness.Context.ConnectedDatabases.SingleAsync(c => c.ConnectionId == connection.ConnectionId);
        persistedConnection.DbAdminId.Should().Be(newAdminId);

        var oldAdminAccess = await harness.Context.UserDatabaseAccesses.SingleAsync(a =>
            a.DatabaseConnectionId == connection.ConnectionId && a.UserId == oldAdminId);
        oldAdminAccess.IsDeleted.Should().BeFalse();
        oldAdminAccess.HasFullAccess.Should().BeTrue();
        oldAdminAccess.RevokedAt.Should().BeNull();

        var newAdminAccess = await harness.Context.UserDatabaseAccesses.SingleAsync(a =>
            a.DatabaseConnectionId == connection.ConnectionId && a.UserId == newAdminId);
        newAdminAccess.IsDeleted.Should().BeTrue();
        newAdminAccess.RevokedByUserId.Should().Be(oldAdminId);

        harness.AuditLogRepository.OwnershipTransferCalls.Should().Be(1);
        harness.AuditLogRepository.LastOwnershipPreviousAdminUserId.Should().Be(oldAdminId);
        harness.AuditLogRepository.LastOwnershipNewAdminUserId.Should().Be(newAdminId);
    }

    private sealed class TestHarness : IAsyncDisposable
    {
        private TestHarness(
            ApplicationDbContext context,
            SqliteConnection sqliteConnection,
            FakeUserRepository userRepository,
            FakeDatabaseExecutor databaseExecutor,
            FakeConnectionStringBuilder connectionStringBuilder,
            FakeEncryptionService encryptionService,
            FakeAuditLogRepository auditLogRepository,
            ConnectionManagementService service)
        {
            Context = context;
            SqliteConnection = sqliteConnection;
            UserRepository = userRepository;
            DatabaseExecutor = databaseExecutor;
            ConnectionStringBuilder = connectionStringBuilder;
            EncryptionService = encryptionService;
            AuditLogRepository = auditLogRepository;
            Service = service;
        }

        public ApplicationDbContext Context { get; }
        public SqliteConnection SqliteConnection { get; }
        public FakeUserRepository UserRepository { get; }
        public FakeDatabaseExecutor DatabaseExecutor { get; }
        public FakeConnectionStringBuilder ConnectionStringBuilder { get; }
        public FakeEncryptionService EncryptionService { get; }
        public FakeAuditLogRepository AuditLogRepository { get; }
        public ConnectionManagementService Service { get; }

        public static async Task<TestHarness> CreateAsync()
        {
            var sqliteConnection = new SqliteConnection("Filename=:memory:");
            await sqliteConnection.OpenAsync();

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseSqlite(sqliteConnection)
                .EnableSensitiveDataLogging()
                .Options;

            var context = new ApplicationDbContext(options);
            await context.Database.EnsureCreatedAsync();

            var userRepository = new FakeUserRepository();
            var databaseExecutor = new FakeDatabaseExecutor();
            var connectionStringBuilder = new FakeConnectionStringBuilder();
            var encryptionService = new FakeEncryptionService();
            var auditLogRepository = new FakeAuditLogRepository();

            var service = new ConnectionManagementService(
                databaseExecutor,
                connectionStringBuilder,
                encryptionService,
                context,
                userRepository,
                auditLogRepository,
                NullLogger<ConnectionManagementService>.Instance);

            return new TestHarness(
                context,
                sqliteConnection,
                userRepository,
                databaseExecutor,
                connectionStringBuilder,
                encryptionService,
                auditLogRepository,
                service);
        }

        public async Task SeedUserAsync(string id, string email, string userName)
        {
            UserRepository.AddUser(new userDto
            {
                Id = id,
                Email = email,
                UserName = userName
            });

            if (!await Context.Users.AnyAsync(user => user.Id == id))
            {
                Context.Users.Add(new ApplicationUser
                {
                    Id = id,
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    UserName = userName,
                    NormalizedUserName = userName.ToUpperInvariant(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString("N")
                });
                await Context.SaveChangesAsync();
            }
        }

        public async Task<ConnectedDatabase> SeedConnectionAsync(
            Guid connectionId,
            string adminUserId,
            DateTime? createdAt = null,
            bool isDeleted = false)
        {
            var connection = new ConnectedDatabase
            {
                ConnectionId = connectionId,
                CreatedByUserId = adminUserId,
                DbAdminId = adminUserId,
                ConnectionName = $"conn-{connectionId:N}",
                DatabaseName = "db_main",
                DatabaseProvider = DbProviders.PostgreSql,
                Host = "localhost",
                PortNumber = 5432,
                Username = "db-user",
                EncryptedPassword = "enc::secret",
                EncryptedRawConnectionString = "enc::Host=localhost;Port=5432;Database=db_main;Username=db-user;Password=secret",
                UseSSL = false,
                UsesRawConnectionString = false,
                LastSuccessfulConnection = DateTime.UtcNow,
                IsHealthy = true,
                CreatedAt = createdAt ?? DateTime.UtcNow,
                IsDeleted = isDeleted
            };

            Context.ConnectedDatabases.Add(connection);
            await Context.SaveChangesAsync();
            return connection;
        }

        public async Task<UserDatabaseAccess> SeedUserAccessAsync(
            Guid connectionId,
            string userId,
            string grantedByUserId,
            bool hasFullAccess,
            DateTime? revokedAt = null)
        {
            var access = new UserDatabaseAccess
            {
                Id = Guid.NewGuid(),
                DatabaseConnectionId = connectionId,
                UserId = userId,
                GrantedByUserId = grantedByUserId,
                GrantedAt = DateTime.UtcNow,
                HasFullAccess = hasFullAccess,
                RestrictedTablesJson = hasFullAccess ? null : "[]",
                IsDeleted = false,
                RevokedAt = revokedAt,
                RevokedByUserId = revokedAt is null ? null : grantedByUserId
            };

            Context.UserDatabaseAccesses.Add(access);
            await Context.SaveChangesAsync();
            return access;
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await SqliteConnection.DisposeAsync();
        }
    }

    private sealed class FakeDatabaseExecutor : IDatabaseExecutor
    {
        public ConnectionTestResult NextTestConnectionResult { get; set; } = new()
        {
            Success = true,
            DatabaseName = "db_main",
            ResponseTimeMs = 5
        };

        public DatabaseQueryResult NextQueryResult { get; set; } = new()
        {
            Success = true,
            RowsReturned = 0
        };

        public int TestConnectionCalls { get; private set; }
        public ConnectedDatabase? LastTestConnectionInput { get; private set; }

        public Task<DatabaseQueryResult> ExecuteQueryAsync(ConnectedDatabase connection, string sql, CancellationToken cancellationToken)
        {
            return Task.FromResult(NextQueryResult);
        }

        public Task<ConnectionTestResult> TestConnectionAsync(ConnectedDatabase connection, CancellationToken cancellationToken)
        {
            TestConnectionCalls++;
            LastTestConnectionInput = connection;
            return Task.FromResult(NextTestConnectionResult);
        }
    }

    private sealed class FakeConnectionStringBuilder : IConnectionStringBuilder
    {
        public string BuildResult { get; set; } = "Host=localhost;Port=5432;Database=db;Username=user;Password=pass";
        public string LastBuildPassword { get; private set; } = string.Empty;

        public ConnectionStringValidationResult ValidationResult { get; set; } = new()
        {
            IsValid = true,
            ParsedComponents = new ConnectionComponents
            {
                Host = "localhost",
                Port = 5432,
                DatabaseName = "db",
                Username = "user",
                UseSSL = false
            }
        };

        public string BuildConnectionString(
            DbProviders provider,
            string host,
            int port,
            string database,
            string username,
            string password,
            bool useSSL,
            string? additionalParameters)
        {
            LastBuildPassword = password;
            return BuildResult;
        }

        public ConnectionComponents? ParseConnectionString(string connectionString, DbProviders provider)
        {
            return ValidationResult.ParsedComponents;
        }

        public ConnectionStringValidationResult ValidateConnectionString(string connectionString, DbProviders provider)
        {
            return ValidationResult;
        }
    }

    private sealed class FakeEncryptionService : IEncryptionService
    {
        public List<string> EncryptInputs { get; } = [];

        public string Encrypt(string plainText)
        {
            EncryptInputs.Add(plainText);
            return $"enc::{plainText}";
        }

        public string Decrypt(string encryptedText)
        {
            if (encryptedText.StartsWith("enc::", StringComparison.Ordinal))
            {
                return encryptedText[5..];
            }

            return encryptedText;
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly Dictionary<string, userDto> _usersById = new(StringComparer.Ordinal);
        private readonly Dictionary<string, userDto> _usersByEmail = new(StringComparer.OrdinalIgnoreCase);

        public void AddUser(userDto user)
        {
            _usersById[user.Id] = user;
            _usersByEmail[user.Email] = user;
        }

        public Task<userDto?> GetByIdAsync(string userId, CancellationToken cancellationToken)
        {
            _usersById.TryGetValue(userId, out var user);
            return Task.FromResult(user);
        }

        public Task<userDto?> GetByEmailAsync(string email, CancellationToken cancellationToken)
        {
            _usersByEmail.TryGetValue(email, out var user);
            return Task.FromResult(user);
        }

        public Task<IReadOnlyList<userDto>> GetByIdsAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken)
        {
            var users = userIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct(StringComparer.Ordinal)
                .Select(id => _usersById.TryGetValue(id, out var user) ? user : null)
                .Where(user => user is not null)
                .Cast<userDto>()
                .ToList();

            return Task.FromResult<IReadOnlyList<userDto>>(users);
        }

        public Task<Result> UpdateUserAsync(userDto user, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> RemoveUserAsync(string userId, CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());
    }

    private sealed class FakeAuditLogRepository : IAuditLogRepository
    {
        public int OwnershipTransferCalls { get; private set; }
        public Guid LastOwnershipConnectionId { get; private set; }
        public string LastOwnershipPreviousAdminUserId { get; private set; } = string.Empty;
        public string LastOwnershipNewAdminUserId { get; private set; } = string.Empty;

        public Task<Result> LogAccessGrantedAsync(
            Guid connectionId,
            string actorUserId,
            string targetUserId,
            bool hasFullAccess,
            IReadOnlyList<string>? restrictedTables,
            CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> LogRestrictionsUpdatedAsync(
            Guid connectionId,
            string actorUserId,
            string targetUserId,
            bool previousFullAccess,
            bool newFullAccess,
            IReadOnlyList<string>? previousRestrictions,
            IReadOnlyList<string>? newRestrictions,
            CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> LogAccessRevokedAsync(
            Guid connectionId,
            string actorUserId,
            string targetUserId,
            CancellationToken cancellationToken)
            => Task.FromResult(Result.Success());

        public Task<Result> LogOwnershipTransferAsync(
            Guid connectionId,
            string previousAdminUserId,
            string newAdminUserId,
            CancellationToken cancellationToken)
        {
            OwnershipTransferCalls++;
            LastOwnershipConnectionId = connectionId;
            LastOwnershipPreviousAdminUserId = previousAdminUserId;
            LastOwnershipNewAdminUserId = newAdminUserId;
            return Task.FromResult(Result.Success());
        }

        public Task<Result<PaginatedAuditLogs>> GetConnectionAuditLogsAsync(
            Guid connectionId,
            int pageNumber,
            int pageSize,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(Result<PaginatedAuditLogs>.Success(new PaginatedAuditLogs
            {
                Items = new List<AuditLogDto>(),
                TotalCount = 0,
                PageNumber = pageNumber,
                PageSize = pageSize
            }));
        }
    }
}
