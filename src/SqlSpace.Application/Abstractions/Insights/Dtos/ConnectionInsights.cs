using SqlSpace.Application.DTOs.Query;

namespace SqlSpace.Application.Abstractions.Insights;

public sealed class ConnectionInsights
{
    public InsightsSummary Summary { get; set; } = new();
    public IReadOnlyList<InsightVolumeBucket> Volume { get; set; } = new List<InsightVolumeBucket>();
    public IReadOnlyList<TableQueryCount> TopTables { get; set; } = new List<TableQueryCount>();
    public IReadOnlyList<ConnectionQueryCount> TopConnections { get; set; } = new List<ConnectionQueryCount>();
    public IReadOnlyList<UserQueryCount> TopUsers { get; set; } = new List<UserQueryCount>();
    public IReadOnlyList<InsightChartCard> Cards { get; set; } = new List<InsightChartCard>();
}
