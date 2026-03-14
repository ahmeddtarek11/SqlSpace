using SqlSpace.Application.DTOs.Query;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.SavedQueries;

public interface ISavedQueryService
{
    Task<Result<IReadOnlyList<SavedQueryDto>>> GetSavedQueriesAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<Result<SavedQueryDto>> CreateSavedQueryAsync(
        CreateSavedQueryRequest request,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<SavedQueryDto>> RenameSavedQueryAsync(
        Guid savedQueryId,
        string name,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<bool>> DeleteSavedQueryAsync(
        Guid savedQueryId,
        string userId,
        CancellationToken cancellationToken);

    Task<Result<QueryExecutionResult>> ExecuteSavedQueryAsync(
        Guid savedQueryId,
        string userId,
        CancellationToken cancellationToken);
}
