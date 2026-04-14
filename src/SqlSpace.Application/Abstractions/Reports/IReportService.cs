using SqlSpace.Application.DTOs.Reports;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Application.Abstractions.Reports;

public interface IReportService
{
    /// <summary>
    /// Generates a full report draft in memory. Nothing is persisted — call SaveAsync to store.
    /// </summary>
    Task<Result<ReportDraftDto>> DraftAsync(
        Guid connectionId,
        string userId,
        string prompt,
        CancellationToken cancellationToken);

    /// <summary>
    /// Persists a (possibly edited) draft as a saved report.
    /// </summary>
    Task<Result<ReportDto>> SaveAsync(
        Guid connectionId,
        string userId,
        CreateReportRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns a saved report with full sections.
    /// </summary>
    Task<Result<ReportDto>> GetAsync(
        Guid connectionId,
        string userId,
        Guid reportId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Returns lightweight headers for the sidebar. No section bodies.
    /// </summary>
    Task<Result<IReadOnlyList<ReportHeaderDto>>> ListAsync(
        Guid connectionId,
        string userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Re-executes every section's SQL and optionally regenerates narrative text.
    /// </summary>
    Task<Result<ReportDto>> RefreshAsync(
        Guid connectionId,
        string userId,
        Guid reportId,
        bool regenerateNarrative,
        CancellationToken cancellationToken);

    /// <summary>
    /// Soft-deletes a report.
    /// </summary>
    Task<Result<bool>> DeleteAsync(
        Guid connectionId,
        string userId,
        Guid reportId,
        CancellationToken cancellationToken);
}
