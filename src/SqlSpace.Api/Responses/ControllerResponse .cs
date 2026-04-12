namespace SqlSpace.Api.Responses;

public sealed record ApiError(string Code, string Message, string? Target = null);

public sealed class ApiResponse<T>
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IReadOnlyList<ApiError> Errors { get; init; } = Array.Empty<ApiError>();
    public string TraceId { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;

    public static ApiResponse<T> Successful(T? data, int statusCode, string traceId, string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = true,
            StatusCode = statusCode,
            Message = message ?? "Request completed successfully.",
            Data = data,
            Errors = Array.Empty<ApiError>(),
            TraceId = traceId
        };
    }

    public static ApiResponse<T> Failed(
        int statusCode,
        IReadOnlyList<ApiError> errors,
        string traceId,
        string? message = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
            StatusCode = statusCode,
            Message = message ?? "Request failed.",
            Data = default,
            Errors = errors,
            TraceId = traceId
        };
    }
}
