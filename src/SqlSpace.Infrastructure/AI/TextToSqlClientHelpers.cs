using System.Text.Json;
namespace SqlSpace.Infrastructure.AI;

internal static class TextToSqlClientHelpers
{
    internal static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static bool TryBuildRoleSchema(
        string schemaJson,
        string provider,
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

                var tableName = table.Name.Trim();
                var schemaName = table.Schema?.Trim() ?? string.Empty;
                var databaseName = snapshot.Database?.Trim() ?? string.Empty;
                var normalizedProvider = provider.Trim().ToLowerInvariant();

                string tableKey;
                if (normalizedProvider == "mysql")
                {
                    if (!string.IsNullOrWhiteSpace(databaseName))
                    {
                        tableKey = $"{databaseName}.{tableName}";
                    }
                    else if (!string.IsNullOrWhiteSpace(schemaName))
                    {
                        tableKey = $"{schemaName}.{tableName}";
                    }
                    else
                    {
                        tableKey = tableName;
                    }
                }
                else
                {
                    tableKey = string.IsNullOrWhiteSpace(schemaName)
                        ? tableName
                        : $"{schemaName}.{tableName}";
                }

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
        out string explanation,
        out TextToSqlErrorPayload? error)
    {
        sql = string.Empty;
        explanation = string.Empty;
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

            if (root.TryGetProperty("explanation", out var explanationElement))
            {
                explanation = explanationElement.GetString() ?? string.Empty;
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
}
