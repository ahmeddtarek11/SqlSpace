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

    private Result(IReadOnlyList<Error> errors, string? message = null)
        : base(false, errors, message)
    {
        Value = default;
    }

    public T? Value { get; }

    public static Result<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new Result<T>(value);
    }

    public static new Result<T> Failure(Error error, string? message = null) => new(ToErrorList(error), message);

    public static new Result<T> Failure(IEnumerable<Error> errors, string? message = null) => new(ToErrorList(errors), message);

    public static implicit operator Result<T>(T value) => Success(value);

    public static implicit operator Result<T>(Error error) => Failure(error);

    public static implicit operator Result<T>(Error[] errors) => Failure(errors);

    public static implicit operator Result<T>(List<Error> errors) => Failure(errors);
}
