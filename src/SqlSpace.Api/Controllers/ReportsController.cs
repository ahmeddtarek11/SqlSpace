using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.Reports;
using SqlSpace.Application.DTOs.Reports;

namespace SqlSpace.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/connections/{connectionId:guid}/reports")]
[Tags("Reports")]
public sealed class ReportsController(
    IReportService reportService,
    ICurrentUserService currentUserService) : ApiController
{
    private readonly IReportService _reportService = reportService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpPost("draft")]
    [EndpointSummary("Generate a report draft from a natural-language prompt")]
    [EndpointDescription("Runs: plan sections → execute SQL per section → narrate each section with real data. Returns an in-memory draft — nothing is saved yet.")]
    [ProducesResponseType(typeof(ApiResponse<ReportDraftDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReportDraftDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ReportDraftDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ReportDraftDto>>> Draft(
        Guid connectionId,
        [FromBody] GenerateReportRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<ReportDraftDto>();

        var result = await _reportService.DraftAsync(connectionId, userId, request.Prompt, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Report draft generated.");
    }

    [HttpPost]
    [EndpointSummary("Save a report draft")]
    [EndpointDescription("Persists the draft (optionally edited by the user) as a saved report with sections.")]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Save(
        Guid connectionId,
        [FromBody] CreateReportRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<ReportDto>();

        var result = await _reportService.SaveAsync(connectionId, userId, request, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status201Created, "Report saved.");
    }

    [HttpGet]
    [EndpointSummary("List saved reports for a connection")]
    [EndpointDescription("Returns lightweight headers only — no section bodies. Ordered newest first.")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ReportHeaderDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ReportHeaderDto>>>> List(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<IReadOnlyList<ReportHeaderDto>>();

        var result = await _reportService.ListAsync(connectionId, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Reports loaded.");
    }

    [HttpGet("{reportId:guid}")]
    [EndpointSummary("Get a saved report with full sections")]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Get(
        Guid connectionId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<ReportDto>();

        var result = await _reportService.GetAsync(connectionId, userId, reportId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Report loaded.");
    }

    [HttpPost("{reportId:guid}/snapshot")]
    [EndpointSummary("Create a new report snapshot with fresh data")]
    [EndpointDescription("Re-executes all SQL and regenerates narratives, then saves a new report. The source report is left unchanged. Fails cleanly if the AI service is unavailable — nothing is saved.")]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ReportDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<ReportDto>>> Snapshot(
        Guid connectionId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<ReportDto>();

        var result = await _reportService.SnapshotAsync(connectionId, userId, reportId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status201Created, "Report snapshot created.");
    }

    [HttpDelete("{reportId:guid}")]
    [EndpointSummary("Delete a saved report")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<bool>>> Delete(
        Guid connectionId,
        Guid reportId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<bool>();

        var result = await _reportService.DeleteAsync(connectionId, userId, reportId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Report deleted.");
    }
}

public class GenerateReportRequest
{
    public string Prompt { get; set; } = string.Empty;
}
