using SqlSpace.Domain.Common.Results;

namespace SqlSpace.Domain.Common.Errors;

public static class AccessControlErrors
{
    public const string InvalidConnectionIdCode = "access_control.invalid_connection_id";
    public const string InvalidAdminUserIdCode = "access_control.invalid_admin_user_id";
    public const string InvalidTargetUserIdCode = "access_control.invalid_target_user_id";
    public const string InvalidTargetUserEmailCode = "access_control.invalid_target_user_email";
    public const string InvalidTableNameCode = "access_control.invalid_table_name";
    public const string ConnectionNotFoundCode = "access_control.connection_not_found";
    public const string AdminNotOwnerCode = "access_control.admin_not_owner";
    public const string TargetUserNotFoundCode = "access_control.target_user_not_found";
    public const string AccessAlreadyExistsCode = "access_control.access_already_exists";
    public const string AccessNotFoundCode = "access_control.access_not_found";
    public const string SchemaSnapshotNotFoundCode = "access_control.schema_snapshot_not_found";
    public const string GrantAccessFailedCode = "access_control.grant_access_failed";
    public const string UpdateRestrictionsFailedCode = "access_control.update_restrictions_failed";
    public const string RevokeAccessFailedCode = "access_control.revoke_access_failed";
    public const string QueryFailedCode = "access_control.query_failed";
    public const string PersistFailedCode = "access_control.persist_failed";
    public const string UnexpectedCode = "access_control.unexpected";

    public const string EmptySchemaNameCode = "access_control.Empty_Schema_Name";

    public static Error InvalidConnectionId(string? target = null) =>
        new(InvalidConnectionIdCode, "Database connection identifier is invalid.", target);

        public static Error EmptySchemaName(string? target = null) =>
        new(EmptySchemaNameCode, "Empty Schema Name for a database provider supporting schema name.", target);

    public static Error InvalidAdminUserId(string? target = null) =>
        new(InvalidAdminUserIdCode, "Admin user identifier is invalid.", target);

    public static Error InvalidTargetUserId(string? target = null) =>
        new(InvalidTargetUserIdCode, "Target user identifier is invalid.", target);

    public static Error InvalidTargetUserEmail(string? target = null) =>
        new(InvalidTargetUserEmailCode, "Target user email is invalid.", target);

    public static Error InvalidTableName(string? target = null) =>
        new(InvalidTableNameCode, "Table name is invalid.", target);

    public static Error ConnectionNotFound(string? target = null) =>
        new(ConnectionNotFoundCode, "Database connection was not found.", target);

    public static Error AdminNotOwner(string? target = null) =>
        new(AdminNotOwnerCode, "Requesting admin user does not own this connection.", target);

    public static Error TargetUserNotFound(string? target = null) =>
        new(TargetUserNotFoundCode, "Target user was not found.", target);

    public static Error AccessAlreadyExists(string? target = null) =>
        new(AccessAlreadyExistsCode, "Target user already has active access to this connection.", target);

    public static Error AccessNotFound(string? target = null) =>
        new(AccessNotFoundCode, "User access grant was not found.", target);

    public static Error SchemaSnapshotNotFound(string? target = null) =>
        new(SchemaSnapshotNotFoundCode, "Schema snapshot was not found for this connection.", target);

    public static Error GrantAccessFailed(string message, string? target = null) =>
        new(GrantAccessFailedCode, message, target);

    public static Error UpdateRestrictionsFailed(string message, string? target = null) =>
        new(UpdateRestrictionsFailedCode, message, target);

    public static Error RevokeAccessFailed(string message, string? target = null) =>
        new(RevokeAccessFailedCode, message, target);

    public static Error QueryFailed(string message, string? target = null) =>
        new(QueryFailedCode, message, target);

    public static Error PersistFailed(string message, string? target = null) =>
        new(PersistFailedCode, message, target);

    public static Error Unexpected(string message, string? target = null) =>
        new(UnexpectedCode, message, target);
}
