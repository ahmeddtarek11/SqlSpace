namespace SqlSpace.Domain.Enums;

public enum QueryStatus
{
    Success = 1,
    Failed = 2,
    InsufficientPermissions = 3 ,
    ValidationFailed = 4,
    LlmError = 5,
    ExecutionFailed = 6,
    Timeout = 7
}
