using SqlSpace.Application.DTOs.Analytics;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.Analytics;

public interface IChartService
{
    Task<Result<IReadOnlyList<ChartSuggestionDto>>> SuggestChartsAsync(
        Guid connectionId,
        string userId,
        string? userPrompt,
        int maxSuggestions,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<SavedChartDto>>> GetChartsForConnectionAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<SavedChartDto>> SaveChartAsync(
        SaveChartRequest request,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<SavedChartDto>> UpdateChartAsync(
        Guid chartId,
        UpdateChartRequest request,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<bool>> DeleteChartAsync(
        Guid chartId,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<ChartDataResult>> ExecuteChartAsync(
        Guid chartId,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<IReadOnlyList<ChartDataResult>>> RefreshAllChartsAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<bool>> UpdateLayoutAsync(
        Guid connectionId,
        string userId,
        IReadOnlyList<ChartLayoutUpdate> layouts,
        CancellationToken cancellationToken);
}
