using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Responses;
using SqlSpace.Application.Abstractions.Auth;
using SqlSpace.Application.Abstractions.Insights;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/connections/{connectionId:guid}/insights")]
[Tags("Insights")]
public sealed class InsightsController(
    IInsightsService insightsService,
    ICurrentUserService currentUserService) : ApiController
{
    private readonly IInsightsService _insightsService = insightsService;
    private readonly ICurrentUserService _currentUserService = currentUserService;

    [HttpGet]
    [EndpointSummary("Get insights for the current user on a connection")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionInsights>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionInsights>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionInsights>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ConnectionInsights>>> GetUserInsights(
        Guid connectionId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? bucket,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<ConnectionInsights>();
        }

        if (!TryParseBucket(bucket, out var bucketValue, out var bucketError))
        {
            return ToApiResponse(Result<ConnectionInsights>.Failure(bucketError), StatusCodes.Status400BadRequest, "Invalid bucket.");
        }

        var query = new InsightsQuery
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Bucket = bucketValue
        };

        var result = await _insightsService.GetUserInsightsAsync(connectionId, userId, query, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Insights loaded.");
    }

    [HttpGet("admin")]
    [EndpointSummary("Get admin insights for a connection")]
    [ProducesResponseType(typeof(ApiResponse<ConnectionInsights>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionInsights>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ConnectionInsights>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<ConnectionInsights>>> GetAdminInsights(
        Guid connectionId,
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        [FromQuery] string? bucket,
        CancellationToken cancellationToken)
    {
        var userId = _currentUserService.GetUserId();
        if (string.IsNullOrWhiteSpace(userId))
        {
            return UnauthorizedResponse<ConnectionInsights>();
        }

        if (!TryParseBucket(bucket, out var bucketValue, out var bucketError))
        {
            return ToApiResponse(Result<ConnectionInsights>.Failure(bucketError), StatusCodes.Status400BadRequest, "Invalid bucket.");
        }

        var query = new InsightsQuery
        {
            DateFrom = dateFrom,
            DateTo = dateTo,
            Bucket = bucketValue
        };

        var result = await _insightsService.GetAdminInsightsAsync(connectionId, userId, query, cancellationToken);
        return ToApiResponse(result, StatusCodes.Status200OK, "Admin insights loaded.");
    }

    private static bool TryParseBucket(
        string? bucket,
        out InsightsBucket bucketValue,
        out Error error)
    {
        error = new Error("insights.invalid_bucket", "Bucket must be one of: day, week, month.", nameof(bucket));

        if (string.IsNullOrWhiteSpace(bucket))
        {
            bucketValue = InsightsBucket.Day;
            return true;
        }

        if (Enum.TryParse<InsightsBucket>(bucket, ignoreCase: true, out var parsed))
        {
            bucketValue = parsed;
            return true;
        }

        bucketValue = InsightsBucket.Day;
        return false;
    }
}
