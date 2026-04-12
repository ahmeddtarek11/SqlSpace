// Application/Services/AccessControl/RestrictedTablesHelper.cs

namespace SqlSpace.Application.Services.AccessControl;

// DTO (already created)
public class TableRestrictionDto
{
    public string Schema { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
}