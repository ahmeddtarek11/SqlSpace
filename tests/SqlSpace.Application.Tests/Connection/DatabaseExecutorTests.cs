using System.Text.Json;
using FluentAssertions;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Application.DTOs.Query;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;
using SqlSpace.Infrastructure.integration;

namespace SqlSpace.Application.Tests.Connection;

public sealed class DatabaseExecutorTests
{
    [Fact]
    public async Task ExecuteQueryAsync_ShouldReturnFailure_WhenConnectionIsNull()
    {
        var factory = new FakeDbConnectionFactory((_, _) => throw new InvalidOperationException("factory should not be used"));
        var sut = CreateSut(factory);

        var result = await sut.ExecuteQueryAsync(null!, "SELECT 1", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("connection cannot be null");
        factory.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldReturnFailure_WhenSqlIsEmpty()
    {
        var factory = new FakeDbConnectionFactory((_, _) => throw new InvalidOperationException("factory should not be used"));
        var sut = CreateSut(factory);

        var result = await sut.ExecuteQueryAsync(NewConnection(), "   ", CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Sql command is null or empty");
        factory.Calls.Should().Be(0);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldReturnSerializedRowsAndColumns_OnSuccess()
    {
        await using var sqlite = await CreateSqliteConnectionWithRowsAsync(2);
        var factory = new FakeDbConnectionFactory((_, _) => Task.FromResult<System.Data.Common.DbConnection>(sqlite));
        var sut = CreateSut(factory);

        var result = await sut.ExecuteQueryAsync(
            NewConnection(),
            "SELECT id, name FROM items ORDER BY id;",
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RowsReturned.Should().Be(2);
        result.ResultsJson.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(result.ResultsJson!);
        var root = doc.RootElement;
        root.GetProperty("columns").EnumerateArray().Select(x => x.GetString()).Should().Equal("id", "name");
        root.GetProperty("rows").GetArrayLength().Should().Be(2);
        root.GetProperty("isTruncated").GetBoolean().Should().BeFalse();
        root.GetProperty("maxRows").GetInt32().Should().Be(2000);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldTruncateResults_AtMaxRows()
    {
        await using var sqlite = await CreateSqliteConnectionWithRowsAsync(2105);
        var factory = new FakeDbConnectionFactory((_, _) => Task.FromResult<System.Data.Common.DbConnection>(sqlite));
        var sut = CreateSut(factory);

        var result = await sut.ExecuteQueryAsync(
            NewConnection(),
            "SELECT id, name FROM items ORDER BY id;",
            CancellationToken.None);

        result.Success.Should().BeTrue();
        result.RowsReturned.Should().Be(2000);
        result.ResultsJson.Should().NotBeNullOrWhiteSpace();

        using var doc = JsonDocument.Parse(result.ResultsJson!);
        var root = doc.RootElement;
        root.GetProperty("rows").GetArrayLength().Should().Be(2000);
        root.GetProperty("isTruncated").GetBoolean().Should().BeTrue();
        root.GetProperty("maxRows").GetInt32().Should().Be(2000);
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldReturnCancelledResult_WhenOperationIsCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var factory = new FakeDbConnectionFactory((_, ct) => throw new OperationCanceledException(ct));
        var sut = CreateSut(factory);

        var result = await sut.ExecuteQueryAsync(NewConnection(), "SELECT 1", cts.Token);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Query execution was cancelled");
    }

    [Fact]
    public async Task ExecuteQueryAsync_ShouldReturnFailure_WhenCommandExecutionThrows()
    {
        await using var sqlite = new SqliteConnection("Filename=:memory:");
        await sqlite.OpenAsync();
        var factory = new FakeDbConnectionFactory((_, _) => Task.FromResult<System.Data.Common.DbConnection>(sqlite));
        var sut = CreateSut(factory);

        var result = await sut.ExecuteQueryAsync(
            NewConnection(),
            "SELECT * FROM missing_table;",
            CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().StartWith("Query execution failed:");
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnFailure_WhenConnectionIsNull()
    {
        var factory = new FakeDbConnectionFactory((_, _) => throw new InvalidOperationException("factory should not be used"));
        var sut = CreateSut(factory);

        var result = await sut.TestConnectionAsync(null!, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Connection cannot be null.");
        factory.Calls.Should().Be(0);
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnSuccess_WhenConnectionCanBeOpened()
    {
        await using var sqlite = new SqliteConnection("Filename=:memory:");
        await sqlite.OpenAsync();
        var factory = new FakeDbConnectionFactory((_, _) => Task.FromResult<System.Data.Common.DbConnection>(sqlite));
        var sut = CreateSut(factory);

        var result = await sut.TestConnectionAsync(NewConnection(databaseName: "fallback-db"), CancellationToken.None);

        result.Success.Should().BeTrue();
        result.DatabaseName.Should().NotBeNullOrWhiteSpace();
        result.ServerVersion.Should().NotBeNullOrWhiteSpace();
        result.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnCancelledResult_WhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var factory = new FakeDbConnectionFactory((_, ct) => throw new OperationCanceledException(ct));
        var sut = CreateSut(factory);

        var result = await sut.TestConnectionAsync(NewConnection(), cts.Token);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Be("Connection test was cancelled.");
    }

    [Fact]
    public async Task TestConnectionAsync_ShouldReturnFailure_WhenFactoryThrows()
    {
        var factory = new FakeDbConnectionFactory((_, _) => throw new InvalidOperationException("boom"));
        var sut = CreateSut(factory);

        var result = await sut.TestConnectionAsync(NewConnection(), CancellationToken.None);

        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("Connection test failed: boom");
    }

    private static DatabaseExecutor CreateSut(IDbConnectionFactory factory)
    {
        return new DatabaseExecutor(
            NullLogger<DatabaseExecutor>.Instance,
            factory);
    }

    private static ConnectedDatabase NewConnection(string databaseName = "db_main")
    {
        return new ConnectedDatabase
        {
            ConnectionId = Guid.NewGuid(),
            CreatedByUserId = "user-1",
            DbAdminId = "admin-1",
            ConnectionName = "test-connection",
            DatabaseProvider = DbProviders.PostgreSql,
            DatabaseName = databaseName,
            Host = "localhost",
            PortNumber = 5432,
            Username = "user",
            EncryptedPassword = "enc-password",
            UsesRawConnectionString = false
        };
    }

    private static async Task<SqliteConnection> CreateSqliteConnectionWithRowsAsync(int rowCount)
    {
        var sqlite = new SqliteConnection("Filename=:memory:");
        await sqlite.OpenAsync();

        await using (var create = sqlite.CreateCommand())
        {
            create.CommandText = "CREATE TABLE items (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT NOT NULL);";
            await create.ExecuteNonQueryAsync();
        }

        await using var tx = await sqlite.BeginTransactionAsync();
        await using (var insert = sqlite.CreateCommand())
        {
            insert.Transaction = (SqliteTransaction?)tx;
            insert.CommandText = "INSERT INTO items (name) VALUES ($name);";
            var nameParam = insert.CreateParameter();
            nameParam.ParameterName = "$name";
            insert.Parameters.Add(nameParam);

            for (var i = 1; i <= rowCount; i++)
            {
                nameParam.Value = $"item-{i}";
                await insert.ExecuteNonQueryAsync();
            }
        }

        await tx.CommitAsync();
        return sqlite;
    }

    private sealed class FakeDbConnectionFactory(
        Func<ConnectedDatabase, CancellationToken, Task<System.Data.Common.DbConnection>> handler)
        : IDbConnectionFactory
    {
        private readonly Func<ConnectedDatabase, CancellationToken, Task<System.Data.Common.DbConnection>> _handler = handler;

        public int Calls { get; private set; }

        public Task<System.Data.Common.DbConnection> CreateOpenConnectionAsync(
            ConnectedDatabase connection,
            CancellationToken cancellationToken)
        {
            Calls++;
            return _handler(connection, cancellationToken);
        }
    }
}
