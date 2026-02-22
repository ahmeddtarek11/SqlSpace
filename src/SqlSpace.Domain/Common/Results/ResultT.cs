namespace SqlSpace.Domain.Common.Results;

/// <summary>
/// Represents a success/failure outcome with payload.
/// </summary>
/// <typeparam name="T">Success payload type.</typeparam>
public sealed class Result<T> : Result
{
    private Result(T value)
        : base(true, Array.Empty<Error>())
    {
        Value = value;
    }

    private Result(IReadOnlyList<Error> errors)
        : base(false, errors)
    {
        Value = default;
    }

    public T? Value { get; }

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(value);
    }

    public static new Result<T> Failure(Error error) => new(ToErrorList(error));

    public static new Result<T> Failure(IEnumerable<Error> errors) => new(ToErrorList(errors));
}
