using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.Insights;

public interface IInsightsService
{
    Task<Result<ConnectionInsights>> GetUserInsightsAsync(
        Guid connectionId,
        string userId,
        InsightsQuery query,
        CancellationToken cancellationToken);

    Task<Result<ConnectionInsights>> GetAdminInsightsAsync(
        Guid connectionId,
        string adminUserId,
        InsightsQuery query,
        CancellationToken cancellationToken);
}
