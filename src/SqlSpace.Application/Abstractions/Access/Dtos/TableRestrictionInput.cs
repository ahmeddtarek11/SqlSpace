namespace SqlSpace.Application.Abstractions.Access;

/// <summary>
/// Table restriction input for access grant/update.
/// </summary>
public class TableRestrictionInput
{
    public string Table { get; set; } = string.Empty;
    public string Schema { get; set; } =string.Empty;
}
