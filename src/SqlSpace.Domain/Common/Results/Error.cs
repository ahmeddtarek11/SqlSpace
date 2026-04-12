namespace SqlSpace.Domain.Common.Results;

/// <summary>
/// Machine-readable error with optional field or source context.
/// </summary>
/// <param name="Code">Stable error code for branching/handling.</param>
/// <param name="Message">Human-readable error message.</param>
/// <param name="Target">Optional field, argument, or subsystem name.</param>
public sealed record Error(string Code, string Message, string? Target = null);
