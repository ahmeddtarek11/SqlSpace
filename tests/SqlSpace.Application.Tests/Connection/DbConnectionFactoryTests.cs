using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSpace.Application.Abstractions.Integrations;
using SqlSpace.Application.Abstractions.Security;
using SqlSpace.Application.DTOs.Connection;
using SqlSpace.Domain.Enums;
using SqlSpace.Domain.Models;
using SqlSpace.Infrastructure.Connection;

namespace SqlSpace.Application.Tests.Connection;

public sealed class DbConnectionFactoryTests
{
    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldThrowArgumentNullException_WhenConnectionIsNull()
    {
        var encryption = new FakeEncryptionService();
        var builder = new FakeConnectionStringBuilder();
        var sut = CreateSut(encryption, builder);

        Func<Task> act = () => sut.CreateOpenConnectionAsync(null!, CancellationToken.None);

        var exception = await act.Should().ThrowAsync<ArgumentNullException>();
        exception.Which.ParamName.Should().Be("connection");
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldThrowInvalidOperationException_WhenRawModeHasNoEncryptedRawString()
    {
        var encryption = new FakeEncryptionService();
        var builder = new FakeConnectionStringBuilder();
        var sut = CreateSut(encryption, builder);
        var connection = NewBaseConnection();
        connection.UsesRawConnectionString = true;
        connection.EncryptedRawConnectionString = null;

        Func<Task> act = () => sut.CreateOpenConnectionAsync(connection, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*raw connection string mode*");
        encryption.DecryptInputs.Should().BeEmpty();
        builder.BuildCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldThrowInvalidOperationException_WhenRawModeHasEmptyEncryptedRawString()
    {
        var encryption = new FakeEncryptionService();
        var builder = new FakeConnectionStringBuilder();
        var sut = CreateSut(encryption, builder);
        var connection = NewBaseConnection();
        connection.UsesRawConnectionString = true;
        connection.EncryptedRawConnectionString = string.Empty;

        Func<Task> act = () => sut.CreateOpenConnectionAsync(connection, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*raw connection string mode*");
        encryption.DecryptInputs.Should().BeEmpty();
        builder.BuildCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldThrowInvalidOperationException_WhenComponentModeHostIsMissing()
    {
        var encryption = new FakeEncryptionService();
        var builder = new FakeConnectionStringBuilder();
        var sut = CreateSut(encryption, builder);
        var connection = NewBaseConnection();
        connection.UsesRawConnectionString = false;
        connection.EncryptedPassword = "enc-password";
        connection.Host = null;

        Func<Task> act = () => sut.CreateOpenConnectionAsync(connection, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*missing host*");
        encryption.DecryptInputs.Should().BeEmpty();
        builder.BuildCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldThrowInvalidOperationException_WhenComponentModePasswordIsMissing()
    {
        var encryption = new FakeEncryptionService();
        var builder = new FakeConnectionStringBuilder();
        var sut = CreateSut(encryption, builder);
        var connection = NewBaseConnection();
        connection.UsesRawConnectionString = false;
        connection.EncryptedPassword = null;
        connection.Host = "localhost";
        connection.Username = "user";
        connection.DatabaseName = "db";

        Func<Task> act = () => sut.CreateOpenConnectionAsync(connection, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*missing encrypted password*");
        encryption.DecryptInputs.Should().BeEmpty();
        builder.BuildCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldThrowInvalidOperationException_WhenComponentModeUsernameIsMissing()
    {
        var encryption = new FakeEncryptionService();
        var builder = new FakeConnectionStringBuilder();
        var sut = CreateSut(encryption, builder);
        var connection = NewBaseConnection();
        connection.UsesRawConnectionString = false;
        connection.EncryptedPassword = "enc-password";
        connection.Host = "localhost";
        connection.Username = null;
        connection.DatabaseName = "db";

        Func<Task> act = () => sut.CreateOpenConnectionAsync(connection, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*missing username*");
        encryption.DecryptInputs.Should().BeEmpty();
        builder.BuildCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldThrowInvalidOperationException_WhenComponentModeDatabaseIsMissing()
    {
        var encryption = new FakeEncryptionService();
        var builder = new FakeConnectionStringBuilder();
        var sut = CreateSut(encryption, builder);
        var connection = NewBaseConnection();
        connection.UsesRawConnectionString = false;
        connection.EncryptedPassword = "enc-password";
        connection.Host = "localhost";
        connection.Username = "user";
        connection.DatabaseName = string.Empty;

        Func<Task> act = () => sut.CreateOpenConnectionAsync(connection, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*missing database name*");
        encryption.DecryptInputs.Should().BeEmpty();
        builder.BuildCallCount.Should().Be(0);
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldBuildFromComponentFields_WithDecryptedPasswordAndDefaultPort()
    {
        var encryption = new FakeEncryptionService();
        encryption.AddDecryption("enc-password", "plain-password");
        var builder = new FakeConnectionStringBuilder();
        var sut = CreateSut(encryption, builder);

        var connection = NewBaseConnection();
        connection.DatabaseProvider = (DbProviders)999;
        connection.UsesRawConnectionString = false;
        connection.EncryptedPassword = "enc-password";
        connection.Host = "db.example.local";
        connection.PortNumber = null;
        connection.DatabaseName = "sales";
        connection.Username = "sql-user";
        connection.UseSSL = true;
        connection.AdditionalParameters = "Pooling=true";

        Func<Task> act = () => sut.CreateOpenConnectionAsync(connection, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*not supported yet*");

        encryption.DecryptInputs.Should().ContainSingle().Which.Should().Be("enc-password");
        builder.BuildCallCount.Should().Be(1);
        builder.LastProvider.Should().Be((DbProviders)999);
        builder.LastHost.Should().Be("db.example.local");
        builder.LastPort.Should().Be(5432);
        builder.LastDatabase.Should().Be("sales");
        builder.LastUsername.Should().Be("sql-user");
        builder.LastPassword.Should().Be("plain-password");
        builder.LastUseSsl.Should().BeTrue();
        builder.LastAdditionalParameters.Should().Be("Pooling=true");
    }

    [Fact]
    public async Task CreateOpenConnectionAsync_ShouldUseRawConnectionStringPath_AndSkipBuilder()
    {
        var encryption = new FakeEncryptionService();
        encryption.AddDecryption("enc-raw", "Host=localhost;Port=5432;Database=db;Username=user;Password=pwd");
        var builder = new FakeConnectionStringBuilder();
        var sut = CreateSut(encryption, builder);

        var connection = NewBaseConnection();
        connection.DatabaseProvider = (DbProviders)999;
        connection.UsesRawConnectionString = true;
        connection.EncryptedRawConnectionString = "enc-raw";
        connection.EncryptedPassword = "enc-password";

        Func<Task> act = () => sut.CreateOpenConnectionAsync(connection, CancellationToken.None);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*not supported yet*");

        encryption.DecryptInputs.Should().ContainSingle().Which.Should().Be("enc-raw");
        builder.BuildCallCount.Should().Be(0);
    }

    private static DbConnectionFactory CreateSut(
        IEncryptionService encryptionService,
        IConnectionStringBuilder connectionStringBuilder)
    {
        return new DbConnectionFactory(
            encryptionService,
            NullLogger<DbConnectionFactory>.Instance,
            connectionStringBuilder);
    }

    private static ConnectedDatabase NewBaseConnection()
    {
        return new ConnectedDatabase
        {
            ConnectionId = Guid.NewGuid(),
            CreatedByUserId = "user-1",
            DbAdminId = "admin-1",
            ConnectionName = "connection-1",
            DatabaseProvider = DbProviders.PostgreSql,
            DatabaseName = "db",
            Host = "localhost",
            Username = "user"
        };
    }

    private sealed class FakeEncryptionService : IEncryptionService
    {
        private readonly Dictionary<string, string> _decryptMap = new(StringComparer.Ordinal);

        public List<string> DecryptInputs { get; } = [];

        public string Encrypt(string plainText)
        {
            throw new NotSupportedException();
        }

        public string Decrypt(string encryptedText)
        {
            DecryptInputs.Add(encryptedText);

            if (_decryptMap.TryGetValue(encryptedText, out var value))
            {
                return value;
            }

            throw new InvalidOperationException($"No fake decryption mapping configured for '{encryptedText}'.");
        }

        public void AddDecryption(string encryptedValue, string plainValue)
        {
            _decryptMap[encryptedValue] = plainValue;
        }
    }

    private sealed class FakeConnectionStringBuilder : IConnectionStringBuilder
    {
        public int BuildCallCount { get; private set; }
        public DbProviders LastProvider { get; private set; }
        public string LastHost { get; private set; } = string.Empty;
        public int LastPort { get; private set; }
        public string LastDatabase { get; private set; } = string.Empty;
        public string LastUsername { get; private set; } = string.Empty;
        public string LastPassword { get; private set; } = string.Empty;
        public bool LastUseSsl { get; private set; }
        public string? LastAdditionalParameters { get; private set; }

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
            BuildCallCount++;
            LastProvider = provider;
            LastHost = host;
            LastPort = port;
            LastDatabase = database;
            LastUsername = username;
            LastPassword = password;
            LastUseSsl = useSSL;
            LastAdditionalParameters = additionalParameters;

            return "Host=localhost;Port=5432;Database=fake;Username=fake;Password=fake";
        }

        public ConnectionComponents? ParseConnectionString(string connectionString, DbProviders provider)
        {
            throw new NotSupportedException();
        }

        public ConnectionStringValidationResult ValidateConnectionString(string connectionString, DbProviders provider)
        {
            throw new NotSupportedException();
        }
    }
}
