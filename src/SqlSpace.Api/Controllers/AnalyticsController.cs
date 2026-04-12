using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Analytics;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.DTOs.Analytics;

namespace SqlSpace.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/connections/{connectionId:guid}/analytics")]
[Tags("Analytics")]
public sealed class AnalyticsController(
    IChartService chartService,
    ICurrentUserService currentUserService) : ApiController
{
    private readonly IChartService _chartService = chartService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpPost("suggest")]
    [EndpointSummary("Get AI-generated chart suggestions for a connection")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChartSuggestionDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ChartSuggestionDto>>>> SuggestCharts(
        Guid connectionId,
        [FromBody] SuggestChartsRequest? request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<IReadOnlyList<ChartSuggestionDto>>();

        var result = await _chartService.SuggestChartsAsync(
            connectionId, userId, request?.UserPrompt, request?.MaxSuggestions ?? 5, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Chart suggestions generated.");
    }

    [HttpGet("charts")]
    [EndpointSummary("List saved charts for a connection")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<SavedChartDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SavedChartDto>>>> GetCharts(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<IReadOnlyList<SavedChartDto>>();

        var result = await _chartService.GetChartsForConnectionAsync(connectionId, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Charts loaded.");
    }

    [HttpPost("charts")]
    [EndpointSummary("Save a chart to the dashboard")]
    [ProducesResponseType(typeof(ApiResponse<SavedChartDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<SavedChartDto>>> SaveChart(
        Guid connectionId,
        [FromBody] SaveChartRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<SavedChartDto>();

        request.ConnectionId = connectionId;
        var result = await _chartService.SaveChartAsync(request, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status201Created, "Chart saved.");
    }

    [HttpPut("charts/{chartId:guid}")]
    [EndpointSummary("Update a saved chart")]
    [ProducesResponseType(typeof(ApiResponse<SavedChartDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<SavedChartDto>>> UpdateChart(
        Guid connectionId,
        Guid chartId,
        [FromBody] UpdateChartRequest request,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<SavedChartDto>();

        var result = await _chartService.UpdateChartAsync(chartId, request, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Chart updated.");
    }

    [HttpDelete("charts/{chartId:guid}")]
    [EndpointSummary("Delete a saved chart")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> DeleteChart(
        Guid connectionId,
        Guid chartId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<bool>();

        var result = await _chartService.DeleteChartAsync(chartId, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Chart deleted.");
    }

    [HttpPost("charts/{chartId:guid}/execute")]
    [EndpointSummary("Execute a single chart's SQL query")]
    [ProducesResponseType(typeof(ApiResponse<ChartDataResult>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<ChartDataResult>>> ExecuteChart(
        Guid connectionId,
        Guid chartId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<ChartDataResult>();

        var result = await _chartService.ExecuteChartAsync(chartId, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Chart executed.");
    }

    [HttpPost("charts/refresh")]
    [EndpointSummary("Refresh all charts for a connection")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<ChartDataResult>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ChartDataResult>>>> RefreshAllCharts(
        Guid connectionId,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<IReadOnlyList<ChartDataResult>>();

        var result = await _chartService.RefreshAllChartsAsync(connectionId, userId, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "All charts refreshed.");
    }

    [HttpPut("charts/layout")]
    [EndpointSummary("Update chart grid layout positions")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<bool>>> UpdateLayout(
        Guid connectionId,
        [FromBody] IReadOnlyList<ChartLayoutUpdate> layouts,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
            return UnauthorizedResponse<bool>();

        var result = await _chartService.UpdateLayoutAsync(connectionId, userId, layouts, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Layout updated.");
    }
}

public class SuggestChartsRequest
{
    public string? UserPrompt { get; set; }
    public int MaxSuggestions { get; set; } = 5;
}
