using FluentAssertions;
using SqlSpace.Application.Services.Query;

namespace SqlSpace.Application.Tests.Query;

public sealed class SqlValidatorServiceTests
{
    private readonly SqlValidatorService _service = new();

    [Fact]
    public async Task ValidateQueryAsync_ShouldThrow_WhenCancellationRequested()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => _service.ValidateQueryAsync(
            "SELECT 1",
            Array.Empty<string>(),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ValidateQueryAsync_ShouldReturnInvalid_WhenSqlIsEmpty()
    {
        var result = await _service.ValidateQueryAsync(
            " ",
            Array.Empty<string>(),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.IsSelectOnly.Should().BeFalse();
        result.ErrorMessage.Should().Be("SQL query cannot be empty.");
    }

    [Fact]
    public async Task ValidateQueryAsync_ShouldReturnInvalid_WhenNotSelectOrWith()
    {
        var result = await _service.ValidateQueryAsync(
            "UPDATE users SET name = 'x'",
            Array.Empty<string>(),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.IsSelectOnly.Should().BeFalse();
        result.ErrorMessage.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ValidateQueryAsync_ShouldReturnInvalid_WhenWriteKeywordDetected()
    {
        var result = await _service.ValidateQueryAsync(
            "SELECT * FROM users; UPDATE users SET name = 'x'",
            Array.Empty<string>(),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.IsSelectOnly.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateQueryAsync_ShouldReturnInvalid_WhenDangerousKeywordDetected()
    {
        var result = await _service.ValidateQueryAsync(
            "SELECT * FROM users; DROP TABLE users",
            Array.Empty<string>(),
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.IsSelectOnly.Should().BeFalse();
    }

    [Fact]
    public async Task ValidateQueryAsync_ShouldReturnInvalid_WhenUnauthorizedTablesReferenced()
    {
        var result = await _service.ValidateQueryAsync(
            "SELECT * FROM sales.orders",
            new[] { "sales.customers" },
            CancellationToken.None);

        result.IsValid.Should().BeFalse();
        result.IsSelectOnly.Should().BeTrue();
        result.TablesReferenced.Should().ContainSingle().Which.Should().Be("sales.orders");
        result.UnauthorizedTables.Should().ContainSingle().Which.Should().Be("sales.orders");
        result.ErrorMessage.Should().Contain("Access denied");
    }

    [Fact]
    public async Task ValidateQueryAsync_ShouldReturnValid_WhenTablesAreAuthorized()
    {
        var result = await _service.ValidateQueryAsync(
            "SELECT * FROM sales.orders JOIN sales.customers c ON c.id = sales.orders.customer_id",
            new[] { "sales.orders", "sales.customers" },
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.IsSelectOnly.Should().BeTrue();
        result.TablesReferenced.Should().BeEquivalentTo(
            new[] { "sales.orders", "sales.customers" },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task ValidateQueryAsync_ShouldSkipAuthorization_WhenAccessibleTablesEmpty()
    {
        var result = await _service.ValidateQueryAsync(
            "SELECT * FROM sales.orders",
            Array.Empty<string>(),
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.TablesReferenced.Should().ContainSingle().Which.Should().Be("sales.orders");
    }

    [Fact]
    public async Task ValidateQueryAsync_ShouldHandleQuotedAndSchemaQualifiedNames()
    {
        var sql = "SELECT * FROM [dbo].[Orders] o JOIN `Sales`.Customers c ON c.Id = o.CustomerId";

        var result = await _service.ValidateQueryAsync(
            sql,
            new[] { "dbo.Orders", "Sales.Customers" },
            CancellationToken.None);

        result.IsValid.Should().BeTrue();
        result.TablesReferenced.Should().BeEquivalentTo(
            new[] { "dbo.Orders", "Sales.Customers" },
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public void IsSelectOnly_ShouldReturnTrue_ForSelectWithCte()
    {
        var isSelectOnly = _service.IsSelectOnly(
            "WITH cte AS (SELECT 1) SELECT * FROM cte");

        isSelectOnly.Should().BeTrue();
    }

    [Fact]
    public void IsSelectOnly_ShouldReturnFalse_WhenWriteKeywordPresent()
    {
        var isSelectOnly = _service.IsSelectOnly(
            "SELECT * FROM users; DELETE FROM users");

        isSelectOnly.Should().BeFalse();
    }

    [Fact]
    public void ContainsDangerousKeywords_ShouldReturnTrue_WhenDangerousKeywordPresent()
    {
        var contains = _service.ContainsDangerousKeywords(
            "DROP TABLE users");

        contains.Should().BeTrue();
    }

    [Fact]
    public void ContainsDangerousKeywords_ShouldReturnFalse_WhenNoDangerousKeywordPresent()
    {
        var contains = _service.ContainsDangerousKeywords(
            "SELECT * FROM users");

        contains.Should().BeFalse();
    }
}
