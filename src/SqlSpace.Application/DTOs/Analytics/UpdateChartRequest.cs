namespace SqlSpace.Application.DTOs.Analytics;

public class UpdateChartRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? SqlQuery { get; set; }
    public string? ChartType { get; set; }
    public string? ChartConfigJson { get; set; }
}
