namespace SqlSpace.Application.DTOs.Analytics;

public class ChartLayoutUpdate
{
    public Guid ChartId { get; set; }
    public int GridX { get; set; }
    public int GridY { get; set; }
    public int GridW { get; set; }
    public int GridH { get; set; }
}
