namespace SqlSpace.Application.Abstractions.Ai;

// Response payload returned from FastAPI.
public sealed class SqlGenerationResponse
{
    public string Sql { get; init; } = string.Empty;
    public string? Model { get; init; }
    public int? Tokens { get; init; }
    public long? LatencyMs { get; init; }
    public string? Error { get; init; }
}
