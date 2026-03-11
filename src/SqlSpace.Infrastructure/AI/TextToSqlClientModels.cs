using System.Text.Json.Serialization;

namespace SqlSpace.Infrastructure.AI;

internal sealed class TextToSqlRequestPayload
{
    [JsonPropertyName("question")]
    public string Question { get; init; } = string.Empty;

    [JsonPropertyName("db_type")]
    public string DbType { get; init; } = string.Empty;

    [JsonPropertyName("role_schema")]
    public Dictionary<string, Dictionary<string, string>> RoleSchema { get; init; }
        = new(StringComparer.OrdinalIgnoreCase);
}

internal sealed class TextToSqlErrorPayload
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("error_code")]
    public string ErrorCode { get; set; } = string.Empty;

    [JsonPropertyName("error_subcode")]
    public string? ErrorSubcode { get; set; }

    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

internal sealed class SchemaSnapshotPayload
{
    [JsonPropertyName("tables")]
    public List<SchemaTablePayload> Tables { get; set; } = new();
}

internal sealed class SchemaTablePayload
{
    [JsonPropertyName("schema")]
    public string? Schema { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("columns")]
    public List<SchemaColumnPayload> Columns { get; set; } = new();
}

internal sealed class SchemaColumnPayload
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("dataType")]
    public string? DataType { get; set; }
}

internal enum TextToSqlResponseKind
{
    Unknown,
    Success,
    Error
}
