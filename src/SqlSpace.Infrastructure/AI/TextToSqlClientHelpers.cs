using System.Text.Json;
using SqlSpace.Application.DTOs.AI;
using SqlSpace.Domain.Common.Results;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Infrastructure.AI;

internal static class TextToSqlClientHelpers
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static bool TryValidateRequest(SqlGenerationRequest? request, out Error error)
    {
        if (request is null)
        {
            error = new Error("llm.invalid_request", "Request payload cannot be null.", nameof(request));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.UserPrompt))
        {
            error = new Error("llm.invalid_request", "UserPrompt cannot be empty.", nameof(request.UserPrompt));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.SchemaContext))
        {
            error = new Error("llm.invalid_request", "SchemaContext cannot be empty.", nameof(request.SchemaContext));
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.DatabaseProvider))
        {
            error = new Error("llm.invalid_request", "DatabaseProvider is required.", nameof(request.DatabaseProvider));
            return false;
        }

        error = new Error("llm.ok", string.Empty);
        return true;
    }

    internal static Uri? ResolveEndpoint(llmApi options, Uri? baseAddress)
    {
        if (!string.IsNullOrWhiteSpace(options.BaseLink) &&
            Uri.TryCreate(options.BaseLink, UriKind.Absolute, out var absolute))
        {
            return absolute;
        }

        return baseAddress;
    }

    internal static bool TryMapDbType(string provider, out string dbType)
    {
        dbType = string.Empty;

        if (string.IsNullOrWhiteSpace(provider))
        {
            return false;
        }

        var normalized = provider.Trim().ToLowerInvariant();

        if (normalized is "postgres" or "postgresql")
        {
            dbType = "postgres";
            return true;
        }

        if (normalized is "mysql")
        {
            dbType = "mysql";
            return true;
        }

        if (normalized is "sqlserver" or "sql_server" or "mssql")
        {
            dbType = "sqlserver";
            return true;
        }

        if (Enum.TryParse<DbProviders>(provider, ignoreCase: true, out var parsed))
        {
            dbType = parsed switch
            {
                DbProviders.PostgreSql => "postgres",
                DbProviders.MySql => "mysql",
                DbProviders.SqlServer => "sqlserver",
                _ => string.Empty
            };
            return !string.IsNullOrWhiteSpace(dbType);
        }

        return false;
    }

    internal static bool TryBuildRoleSchema(
        string schemaJson,
        out Dictionary<string, Dictionary<string, string>> roleSchema,
        out string error)
    {
        roleSchema = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
        error = string.Empty;

        try
        {
            var direct = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(
                schemaJson,
                JsonOptions);

            if (direct is { Count: > 0 })
            {
                roleSchema = direct;
                return true;
            }
        }
        catch (JsonException)
        {
            // Fall back to snapshot parsing below.
        }

        try
        {
            var snapshot = JsonSerializer.Deserialize<SchemaSnapshotPayload>(schemaJson, JsonOptions);
            if (snapshot?.Tables is null || snapshot.Tables.Count == 0)
            {
                error = "SchemaContext does not contain any tables.";
                return false;
            }

            foreach (var table in snapshot.Tables)
            {
                if (string.IsNullOrWhiteSpace(table.Name))
                {
                    continue;
                }

                var tableKey = table.Name.Trim();

                if (roleSchema.ContainsKey(tableKey))
                {
                    error = $"Duplicate table name '{tableKey}' detected across schemas.";
                    return false;
                }

                var columns = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var column in table.Columns)
                {
                    if (string.IsNullOrWhiteSpace(column.Name))
                    {
                        continue;
                    }

                    var columnName = column.Name.Trim();
                    var columnType = string.IsNullOrWhiteSpace(column.DataType)
                        ? "unknown"
                        : column.DataType.Trim();

                    columns[columnName] = columnType;
                }

                if (columns.Count > 0)
                {
                    roleSchema[tableKey] = columns;
                }
            }

            if (roleSchema.Count == 0)
            {
                error = "SchemaContext did not yield any valid table/column mappings.";
                return false;
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = $"SchemaContext is not valid JSON: {ex.Message}";
            return false;
        }
    }

    internal static TextToSqlResponseKind ParseResponse(
        string responseBody,
        out string sql,
        out TextToSqlErrorPayload? error)
    {
        sql = string.Empty;
        error = null;

        using var document = JsonDocument.Parse(responseBody);
        var root = document.RootElement;

        if (!root.TryGetProperty("status", out var statusElement))
        {
            return TextToSqlResponseKind.Unknown;
        }

        var status = statusElement.GetString();
        if (string.Equals(status, "success", StringComparison.OrdinalIgnoreCase))
        {
            if (root.TryGetProperty("sql", out var sqlElement))
            {
                sql = sqlElement.GetString() ?? string.Empty;
            }

            return TextToSqlResponseKind.Success;
        }

        if (string.Equals(status, "error", StringComparison.OrdinalIgnoreCase))
        {
            error = JsonSerializer.Deserialize<TextToSqlErrorPayload>(responseBody, JsonOptions);
            return TextToSqlResponseKind.Error;
        }

        return TextToSqlResponseKind.Unknown;
    }

    internal static Error ToError(TextToSqlErrorPayload apiError)
    {
        var code = string.IsNullOrWhiteSpace(apiError.ErrorCode)
            ? "llm.error"
            : $"llm.{apiError.ErrorCode.ToLowerInvariant()}";

        var message = string.IsNullOrWhiteSpace(apiError.Message)
            ? "LLM API returned an error."
            : apiError.Message;

        return new Error(code, message, apiError.ErrorSubcode);
    }
}
