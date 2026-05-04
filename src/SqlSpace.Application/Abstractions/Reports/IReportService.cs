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
    /// Creates a new report with fresh SQL results and regenerated narratives from an existing one.
    /// The source report is left unchanged. Fails cleanly if the AI service is unavailable — nothing is saved.
    /// </summary>
    Task<Result<ReportDto>> SnapshotAsync(
        Guid connectionId,
        string userId,
        Guid sourceReportId,
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
