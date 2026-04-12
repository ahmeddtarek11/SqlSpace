using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Domain.Common.Errors;

public static class ConnectionErrors
{
    public const string InvalidRequestCode = "connection.invalid_request";
    public const string UserIdIsRequiredCode = "connection.userid_required";
    public const string InvalidConnectionIdCode = "connection.invalid_connection_id";
    public const string ConnectionNotFoundCode = "connection.not_found";
    public const string AdminNotOwnerCode = "connection.admin_not_owner";
    public const string DeleteConnectionFailedCode = "connection.delete_failed";
    public const string UnsupportedProviderCode = "connection.unsupported_provider";
    public const string UnsupportedInputModeCode = "connection.unsupported_input_mode";
    public const string HostRequiredCode = "connection.host_required";
    public const string DatabaseNameRequiredCode = "connection.database_name_required";
    public const string UsernameRequiredCode = "connection.username_required";
    public const string PasswordRequiredCode = "connection.password_required";
    public const string InvalidPortCode = "connection.invalid_port";
    public const string RawConnectionStringRequiredCode = "connection.raw_connection_string_required";
    public const string InvalidConnectionStringCode = "connection.invalid_connection_string";
    public const string ConnectionTestFailedCode = "connection.test_failed";
    public const string UnexpectedCode = "connection.unexpected";
    public const string InValidUserIdCode = "connection.invalid_userid";


      public static Error InvalidUserId(string userId,string? target = null) =>
        new(InvalidRequestCode, $"Cannot validate the user with Provided id:{userId}.", target);
    public static Error InvalidRequest(string? target = null) =>
        new(InvalidRequestCode, "Connection request is invalid.", target);

    public static Error UserIdRequired(string? target = null) =>
        new(UserIdIsRequiredCode, "User Id is a Required Filed.", target);

    public static Error InvalidConnectionId(string? target = null) =>
        new(InvalidConnectionIdCode, "Connection identifier is invalid.", target);

    public static Error ConnectionNotFound(string connectionId, string? target = null) =>
        new(ConnectionNotFoundCode, $"Connection with id '{connectionId}' was not found.", target);

    public static Error AdminNotOwner(string connectionId, string? target = null) =>
        new(AdminNotOwnerCode, $"User is not allowed to modify connection '{connectionId}'.", target);

    public static Error DeleteConnectionFailed(string message, string? target = null) =>
        new(DeleteConnectionFailedCode, message, target);

    public static Error UnsupportedProvider(string provider, string? target = null) =>
        new(UnsupportedProviderCode, $"Database provider '{provider}' is not supported.", target);

    public static Error UnsupportedInputMode(string mode, string? target = null) =>
        new(UnsupportedInputModeCode, $"Connection input mode '{mode}' is not supported.", target);

    public static Error HostRequired(string? target = null) =>
        new(HostRequiredCode, "Host is required.", target);

    public static Error DatabaseNameRequired(string? target = null) =>
        new(DatabaseNameRequiredCode, "Database name is required.", target);

    public static Error UsernameRequired(string? target = null) =>
        new(UsernameRequiredCode, "Username is required.", target);

    public static Error PasswordRequired(string? target = null) =>
        new(PasswordRequiredCode, "Password is required.", target);

    public static Error InvalidPort(int port, string? target = null) =>
        new(InvalidPortCode, $"Port must be between 1 and 65535. Received: {port}.", target);

    public static Error RawConnectionStringRequired(string? target = null) =>
        new(RawConnectionStringRequiredCode, "Raw connection string is required.", target);

    public static Error InvalidConnectionString(string message, string? target = null) =>
        new(InvalidConnectionStringCode, message, target);

    public static Error ConnectionTestFailed(string message, string? target = null) =>
        new(ConnectionTestFailedCode, message, target);

    public static Error Unexpected(string message, string? target = null) =>
        new(UnexpectedCode, message, target);
}
