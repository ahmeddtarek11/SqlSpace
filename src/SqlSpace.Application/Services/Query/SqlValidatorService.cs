using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SqlSpace.Application.Abstractions.Security;
using SqlSpace.Application.DTOs.Query;

namespace SqlSpace.Application.Services.Query;

public  class SqlValidatorService : ISqlValidator
{
    private readonly ILogger<SqlValidatorService> _logger;

    public SqlValidatorService(ILogger<SqlValidatorService> logger)
    {
        _logger = logger;
    }


    private static readonly Regex DangerousKeywordRegex = new(
        @"\b(INSERT|UPDATE|DELETE|DROP|TRUNCATE|ALTER|CREATE|EXEC(?:UTE)?|MERGE|GRANT|REVOKE|DENY)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex WriteKeywordRegex = new(
        @"\b(INSERT|UPDATE|DELETE|MERGE)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FromJoinRegex = new(
        @"\b(?:FROM|JOIN)\s+(?<table>[A-Za-z0-9_\.\[\]""`]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public Task<SqlValidationResult> ValidateQueryAsync(
        string sql,
        IReadOnlyList<string> accessibleTables,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(sql))
        {
            _logger.LogWarning("SQL validation failed: empty SQL.");
            return Task.FromResult(new SqlValidationResult
            {
                IsValid = false,
                IsSelectOnly = false,
                ErrorMessage = "SQL query cannot be empty."
            });
        }

        if (!IsSelectOnly(sql) || ContainsDangerousKeywords(sql))
        {
            _logger.LogWarning("SQL validation failed: non-SELECT or dangerous keywords detected. SQL: {Sql}", sql.Length > 200 ? sql[..200] : sql);
            return Task.FromResult(new SqlValidationResult
            {
                IsValid = false,
                IsSelectOnly = false,
                ErrorMessage = "Only SELECT queries are allowed. The query contains prohibited keywords."
            });
        }

        var tablesReferenced = ExtractTableNames(sql);

        if (accessibleTables is { Count: > 0 } && tablesReferenced.Count > 0)
        {
            var accessibleSet = new HashSet<string>(accessibleTables, StringComparer.OrdinalIgnoreCase);
            var unauthorizedTables = tablesReferenced
                .Where(t => !accessibleSet.Contains(t))
                .ToList();

            if (unauthorizedTables.Count > 0)
            {
                _logger.LogWarning("SQL validation failed: unauthorized tables accessed. Tables: {UnauthorizedTables}", string.Join(", ", unauthorizedTables));
                return Task.FromResult(new SqlValidationResult
                {
                    IsValid = false,
                    IsSelectOnly = true,
                    TablesReferenced = tablesReferenced,
                    UnauthorizedTables = unauthorizedTables,
                    ErrorMessage = $"Access denied to tables: {string.Join(", ", unauthorizedTables)}"
                });
            }
        }

        _logger.LogDebug("SQL validation passed. TablesReferenced: {Tables}", string.Join(", ", tablesReferenced));
        return Task.FromResult(new SqlValidationResult
        {
            IsValid = true,
            IsSelectOnly = true,
            TablesReferenced = tablesReferenced
        });
    }

    public bool IsSelectOnly(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return false;

        var normalized = sql.TrimStart();

        if (!normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) &&
            !normalized.StartsWith("WITH", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return !WriteKeywordRegex.IsMatch(normalized);
    }

    public bool ContainsDangerousKeywords(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            return false;

        return DangerousKeywordRegex.IsMatch(sql);
    }

    private static IReadOnlyList<string> ExtractTableNames(string sql)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(sql))
            return tables.ToList();

        foreach (Match match in FromJoinRegex.Matches(sql))
        {
            AddExtractedTable(tables, match.Groups["table"].Value);
        }

        return tables.ToList();
    }

    private static void AddExtractedTable(HashSet<string> tables, string rawTable)
    {
        if (string.IsNullOrWhiteSpace(rawTable))
            return;

        var token = rawTable.Trim();
        token = token.TrimEnd(',', ';', ')');

        if (token.Length == 0)
            return;

        var parts = token.Split('.');
        for (int i = 0; i < parts.Length; i++)
        {
            parts[i] = StripQuotes(parts[i].Trim());
        }

        var tableName = string.Join(".", parts);
        if (tableName.Length > 0)
            tables.Add(tableName);
    }

    private static string StripQuotes(string identifier)
    {
        if (identifier.Length >= 2)
        {
            if ((identifier[0] == '"' && identifier[^1] == '"') ||
                (identifier[0] == '[' && identifier[^1] == ']') ||
                (identifier[0] == '`' && identifier[^1] == '`'))
            {
                return identifier[1..^1];
            }
        }

        return identifier;
    }
}
