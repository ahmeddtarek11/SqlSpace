namespace SqlSpace.Domain.Common.Results;

/// <summary>
/// Represents a success/failure outcome without payload.
/// </summary>
public class Result
{
    private static readonly IReadOnlyList<Error> EmptyErrors = Array.Empty<Error>();

    protected Result(bool isSuccess, IReadOnlyList<Error> errors, string? message = null)
    {
        if (isSuccess && errors.Count > 0)
        {
            throw new ArgumentException("Successful result cannot contain errors.", nameof(errors));
        }

        if (!isSuccess && errors.Count == 0)
        {
            throw new ArgumentException("Failed result must contain at least one error.", nameof(errors));
        }

        IsSuccess = isSuccess;
        Errors = errors;
        Message = message;
    }

    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public IReadOnlyList<Error> Errors { get; }
    public string? Message { get; }

    public static Result Success() => new(true, EmptyErrors);

    public static Result Failure(Error error, string? message = null) => new(false, ToErrorList(error), message);

    public static Result Failure(IEnumerable<Error> errors, string? message = null) => new(false, ToErrorList(errors), message);

    public static implicit operator Result(Error error) => Failure(error);

    public static implicit operator Result(Error[] errors) => Failure(errors);

    public static implicit operator Result(List<Error> errors) => Failure(errors);

    protected static IReadOnlyList<Error> ToErrorList(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new[] { error };
    }

    protected static IReadOnlyList<Error> ToErrorList(IEnumerable<Error> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        var errorList = errors.Where(error => error is not null).ToList();
        if (errorList.Count == 0)
        {
            throw new ArgumentException("At least one error is required.", nameof(errors));
        }

        return errorList;
    }
}
