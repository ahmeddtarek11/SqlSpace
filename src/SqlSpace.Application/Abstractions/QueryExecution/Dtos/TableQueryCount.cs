namespace SqlSpace.Application.DTOs.Query;

public class TableQueryCount
{
    public string TableName { get; set; } = string.Empty;
    public int QueryCount { get; set; }
}
