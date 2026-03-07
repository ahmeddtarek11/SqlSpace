using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using SqlSpace.Domain.Enums;
using SqlSpace.Infrastructure.Connection;

namespace SqlSpace.Application.Tests.Connection;

public sealed class ConnectionStringBuilderServiceTests
{
    [Theory]
    [InlineData(DbProviders.PostgreSql, 5432, "Host=localhost;Port=5432;Database=db;Username=user;Password=pass")]
    [InlineData(DbProviders.SqlServer, 1433, "Server=localhost,1433;Database=db;User Id=user;Password=pass")]
    [InlineData(DbProviders.MySql, 3306, "Server=localhost;Port=3306;Database=db;Uid=user;Pwd=pass")]
    public void BuildConnectionString_ShouldGenerateCorrectFormat_ForEachProvider(
        DbProviders provider,
        int port,
        string expected)
    {
        var sut = CreateSut();

        var result = sut.BuildConnectionString(
            provider,
            "localhost",
            port,
            "db",
            "user",
            "pass",
            false,
            null);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(null, "Host=localhost;Port=5432;Database=db;Username=user;Password=pass")]
    [InlineData("", "Host=localhost;Port=5432;Database=db;Username=user;Password=pass")]
    [InlineData("   ", "Host=localhost;Port=5432;Database=db;Username=user;Password=pass")]
    [InlineData(";", "Host=localhost;Port=5432;Database=db;Username=user;Password=pass")]
    [InlineData(";;", "Host=localhost;Port=5432;Database=db;Username=user;Password=pass")]
    [InlineData(";Pooling=true", "Host=localhost;Port=5432;Database=db;Username=user;Password=pass;Pooling=true")]
    [InlineData("Pooling=true;", "Host=localhost;Port=5432;Database=db;Username=user;Password=pass;Pooling=true")]
    [InlineData(";Pooling=true;", "Host=localhost;Port=5432;Database=db;Username=user;Password=pass;Pooling=true")]
    public void BuildConnectionString_ShouldHandleAdditionalParametersEdgeCases_ForPostgreSql(
        string? additionalParameters,
        string expected)
    {
        var sut = CreateSut();

        var result = sut.BuildConnectionString(
            DbProviders.PostgreSql,
            "localhost",
            5432,
            "db",
            "user",
            "pass",
            false,
            additionalParameters);

        result.Should().Be(expected);
    }

    [Fact]
    public void BuildConnectionString_ShouldAppendSslAndTrimAdditionalParameters_ForPostgreSql()
    {
        var sut = CreateSut();

        var result = sut.BuildConnectionString(
            DbProviders.PostgreSql,
            "localhost",
            5432,
            "sales",
            "user1",
            "pass1",
            true,
            " ;Pooling=true;Command Timeout=30;; ");

        result.Should().Be(
            "Host=localhost;Port=5432;Database=sales;Username=user1;Password=pass1;SSL Mode=Require;Pooling=true;Command Timeout=30");
    }

    [Fact]
    public void BuildConnectionString_ShouldEscapeValuesContainingSpecialCharacters()
    {
        var sut = CreateSut();

        var result = sut.BuildConnectionString(
            DbProviders.MySql,
            "db;host",
            3306,
            "db=name",
            "us\"er",
            "p;ss",
            false,
            null);

        result.Should().StartWith(
            "Server=\"db;host\";Port=3306;Database=\"db=name\";Uid=\"us\"\"er\";Pwd=\"p;ss\"");
    }

    [Fact]
    public void BuildConnectionString_ShouldThrowArgumentException_WhenPortIsOutOfRange()
    {
        var sut = CreateSut();

        Action act = () => sut.BuildConnectionString(
            DbProviders.PostgreSql,
            "localhost",
            70000,
            "db",
            "user",
            "pwd",
            false,
            null);

        act.Should().Throw<ArgumentException>()
            .WithParameterName("port");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildConnectionString_ShouldThrowArgumentException_WhenHostIsInvalid(string host)
    {
        var sut = CreateSut();

        Action act = () => sut.BuildConnectionString(
            DbProviders.PostgreSql,
            host,
            5432,
            "db",
            "user",
            "pass",
            false,
            null);

        act.Should().Throw<ArgumentException>().WithParameterName("host");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildConnectionString_ShouldThrowArgumentException_WhenDatabaseIsInvalid(string database)
    {
        var sut = CreateSut();

        Action act = () => sut.BuildConnectionString(
            DbProviders.PostgreSql,
            "localhost",
            5432,
            database,
            "user",
            "pass",
            false,
            null);

        act.Should().Throw<ArgumentException>().WithParameterName("database");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildConnectionString_ShouldThrowArgumentException_WhenUsernameIsInvalid(string username)
    {
        var sut = CreateSut();

        Action act = () => sut.BuildConnectionString(
            DbProviders.PostgreSql,
            "localhost",
            5432,
            "db",
            username,
            "pass",
            false,
            null);

        act.Should().Throw<ArgumentException>().WithParameterName("username");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void BuildConnectionString_ShouldThrowArgumentException_WhenPasswordIsInvalid(string password)
    {
        var sut = CreateSut();

        Action act = () => sut.BuildConnectionString(
            DbProviders.PostgreSql,
            "localhost",
            5432,
            "db",
            "user",
            password,
            false,
            null);

        act.Should().Throw<ArgumentException>().WithParameterName("password");
    }

    [Fact]
    public void ParseConnectionString_ShouldParsePostgreSqlCorrectly()
    {
        var sut = CreateSut();

        var components = sut.ParseConnectionString(
            "Host=pg.local;Port=5433;Database=sales;Username=admin;Password=secret;SSL Mode=Require",
            DbProviders.PostgreSql);

        components.Should().NotBeNull();
        components!.Host.Should().Be("pg.local");
        components.Port.Should().Be(5433);
        components.DatabaseName.Should().Be("sales");
        components.Username.Should().Be("admin");
        components.Password.Should().Be("secret");
        components.UseSSL.Should().BeTrue();
    }

    [Fact]
    public void ParseConnectionString_ShouldParseSqlServerServerPortAndAdditionalParameters()
    {
        var sut = CreateSut();

        var components = sut.ParseConnectionString(
            "Server=sql.prod.local,1544;Database=core;User Id=sa;Password=topsecret;Encrypt=True;TrustServerCertificate=False;Application Name=SqlSpace",
            DbProviders.SqlServer);

        components.Should().NotBeNull();
        components!.Host.Should().Be("sql.prod.local");
        components.Port.Should().Be(1544);
        components.DatabaseName.Should().Be("core");
        components.Username.Should().Be("sa");
        components.Password.Should().Be("topsecret");
        components.UseSSL.Should().BeTrue();
        components.AdditionalParameters.Should().Contain("TrustServerCertificate=False");
        components.AdditionalParameters.Should().Contain("Application Name=SqlSpace");
    }

    [Fact]
    public void ParseConnectionString_ShouldParseMySqlCorrectly()
    {
        var sut = CreateSut();

        var components = sut.ParseConnectionString(
            "Server=mysql.local;Port=3307;Database=shop;Uid=root;Pwd=topsecret;SslMode=Required;AllowPublicKeyRetrieval=true",
            DbProviders.MySql);

        components.Should().NotBeNull();
        components!.Host.Should().Be("mysql.local");
        components.Port.Should().Be(3307);
        components.DatabaseName.Should().Be("shop");
        components.Username.Should().Be("root");
        components.Password.Should().Be("topsecret");
        components.UseSSL.Should().BeTrue();
        components.AdditionalParameters.Should().Contain("AllowPublicKeyRetrieval=true");
    }

    [Fact]
    public void ParseConnectionString_ShouldUseDefaultSqlServerPort_WhenPortIsMissing()
    {
        var sut = CreateSut();

        var components = sut.ParseConnectionString(
            "Server=sql.prod.local;Database=core;User Id=sa;Password=topsecret;Encrypt=True",
            DbProviders.SqlServer);

        components.Should().NotBeNull();
        components!.Port.Should().Be(1433);
    }

    [Fact]
    public void ParseConnectionString_ShouldHandleQuotedValuesWithSemicolons()
    {
        var sut = CreateSut();

        var components = sut.ParseConnectionString(
            "Host=\"db;server\";Port=5432;Database=\"db;name\";Username=\"us;er\";Password=\"p;ss\"",
            DbProviders.PostgreSql);

        components.Should().NotBeNull();
        components!.Host.Should().Be("db;server");
        components.DatabaseName.Should().Be("db;name");
        components.Username.Should().Be("us;er");
        components.Password.Should().Be("p;ss");
    }

    [Theory]
    [InlineData(DbProviders.PostgreSql, "SSL Mode=Require", true)]
    [InlineData(DbProviders.PostgreSql, "SSL Mode=Prefer", false)]
    [InlineData(DbProviders.SqlServer, "Encrypt=True", true)]
    [InlineData(DbProviders.SqlServer, "Encrypt=False", false)]
    [InlineData(DbProviders.MySql, "SslMode=Required", true)]
    [InlineData(DbProviders.MySql, "SslMode=Preferred", false)]
    public void ParseConnectionString_ShouldDetectSslCorrectly(
        DbProviders provider,
        string sslParameter,
        bool expectedSsl)
    {
        var sut = CreateSut();
        var connectionString = BuildConnectionStringForSslTest(provider, sslParameter);

        var components = sut.ParseConnectionString(connectionString, provider);

        components.Should().NotBeNull();
        components!.UseSSL.Should().Be(expectedSsl);
    }

    [Fact]
    public void ParseConnectionString_ShouldReturnNull_WhenRequiredFieldsAreMissing()
    {
        var sut = CreateSut();

        var components = sut.ParseConnectionString(
            "Host=localhost;Port=5432;Username=user1",
            DbProviders.PostgreSql);

        components.Should().BeNull();
    }

    [Fact]
    public void ValidateConnectionString_ShouldReturnValidResult_WithParsedComponents()
    {
        var sut = CreateSut();

        var result = sut.ValidateConnectionString(
            "Server=mysql.local;Port=3306;Database=shop;Uid=root;Pwd=secret;SslMode=Required",
            DbProviders.MySql);

        result.IsValid.Should().BeTrue();
        result.ErrorMessage.Should().BeNull();
        result.ParsedComponents.Should().NotBeNull();
        result.ParsedComponents!.Host.Should().Be("mysql.local");
        result.ParsedComponents.UseSSL.Should().BeTrue();
    }

    [Fact]
    public void ValidateConnectionString_ShouldReturnInvalidResult_WhenConnectionStringCannotBeParsed()
    {
        var sut = CreateSut();

        var result = sut.ValidateConnectionString(
            "not-a-connection-string",
            DbProviders.PostgreSql);

        result.IsValid.Should().BeFalse();
        result.ErrorMessage.Should().Be("Failed to parse connection string. Check format.");
        result.ParsedComponents.Should().BeNull();
    }

    private static ConnectionStringBuilderService CreateSut()
    {
        return new ConnectionStringBuilderService(
            NullLogger<ConnectionStringBuilderService>.Instance);
    }

    private static string BuildConnectionStringForSslTest(DbProviders provider, string sslParameter)
    {
        return provider switch
        {
            DbProviders.PostgreSql =>
                $"Host=localhost;Port=5432;Database=db;Username=user;Password=pass;{sslParameter}",
            DbProviders.SqlServer =>
                $"Server=localhost,1433;Database=db;User Id=user;Password=pass;{sslParameter}",
            DbProviders.MySql =>
                $"Server=localhost;Port=3306;Database=db;Uid=user;Pwd=pass;{sslParameter}",
            _ => throw new ArgumentOutOfRangeException(nameof(provider), provider, null)
        };
    }
}
