using SqlSpace.Application.DTOs.Analytics;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.Analytics;

public interface IAnalyticsAiClient
{
    Task<Result<IReadOnlyList<ChartSuggestionDto>>> GetChartSuggestionsAsync(
        string schemaContext,
        string databaseProvider,
        string? userPrompt,
        string? ragContext,
        int maxSuggestions,
        CancellationToken cancellationToken);
}
