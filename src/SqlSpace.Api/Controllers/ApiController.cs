using Microsoft.AspNetCore.Mvc;
using SqlSpace.Api.Responses;
using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Api.Controllers;

[ApiController]
public class ApiController : ControllerBase
{

    /// <summary>
    /// 
    /// </summary>
    /// <param name="result"></param>
    /// <param name="successStatusCode"></param>
    /// <param name="successMessage"></param>
    /// <returns></returns>
    protected ActionResult<ApiResponse<object?>> ToApiResponse(
        Result result,
        int successStatusCode,
        string successMessage)
    {
        if (result.IsSuccess)
        {
            var success = ApiResponse<object?>.Successful(
                data: null,
                statusCode: successStatusCode,
                traceId: HttpContext.TraceIdentifier,
                message: successMessage);

            return StatusCode(successStatusCode, success);
        }

        var errors = result.Errors
            .Select(error => new ApiError(error.Code, error.Message, error.Target))
            .ToArray();

        var failed = ApiResponse<object?>.Failed(
            statusCode: StatusCodes.Status400BadRequest,
            errors: errors,
            traceId: HttpContext.TraceIdentifier);

        return BadRequest(failed);
    }

    
/// <summary>
///
/// </summary>
/// <typeparam name="T"></typeparam>
/// <param name="result"></param>
/// <param name="successStatusCode"></param>
/// <param name="successMessage"></param>
/// <returns></returns>
    protected ActionResult<ApiResponse<T>> ToApiResponse<T>(
        Result<T> result,
        int successStatusCode,
        string successMessage)
    {
        if (result.IsSuccess)
        {
            var success = ApiResponse<T>.Successful(
                data: result.Value,
                statusCode: successStatusCode,
                traceId: HttpContext.TraceIdentifier,
                message: successMessage);

            return StatusCode(successStatusCode, success);
        }

        var errors = result.Errors
            .Select(error => new ApiError(error.Code, error.Message, error.Target))
            .ToArray();

        var failed = ApiResponse<T>.Failed(
            statusCode: StatusCodes.Status400BadRequest,
            errors: errors,
            traceId: HttpContext.TraceIdentifier);

        return BadRequest(failed);
    }
}
