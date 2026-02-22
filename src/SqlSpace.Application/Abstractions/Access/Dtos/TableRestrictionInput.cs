namespace SqlSpace.Application.Abstractions.Access;

/// <summary>
/// Table restriction input for access grant/update.
/// </summary>
public class TableRestrictionInput
{
    public string TableName { get; set; } = string.Empty;
    public string? SchemaName { get; set; }
}
