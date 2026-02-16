using SqlSpace.Domain.Enums;

namespace SqlSpace.Application.Abstractions.Ai;

// Request payload sent to FastAPI.
public sealed class SqlGenerationRequest
{
    public string UserPrompt { get; init; } = string.Empty;
    public string FilteredSchemaJson { get; init; } = string.Empty;
    public DbProviders DbProvider { get; init; }
}
