using System;
using SqlSpace.Domain.Enums;

namespace SqlSpace.Domain.Models;

public class SavedChart
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid DatabaseConnectionId { get; set; }

    // Chart identity
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    // The SQL that powers this chart
    public string SqlQuery { get; set; } = string.Empty;
    public string? OriginalPrompt { get; set; }

    // Chart rendering config
    public ChartType ChartType { get; set; } = ChartType.Bar;
    public string ChartConfigJson { get; set; } = "{}";

    // Layout position for dashboard grid
    public int GridX { get; set; } = 0;
    public int GridY { get; set; } = 0;
    public int GridW { get; set; } = 6;
    public int GridH { get; set; } = 4;
    public int SortOrder { get; set; } = 0;

    // Metadata
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public ConnectedDatabase DatabaseConnection { get; set; } = null!;
}
