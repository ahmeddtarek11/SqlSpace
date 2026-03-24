namespace SqlSpace.Domain.Enums;

public enum ChartType
{
    // Bar family
    Bar = 1,
    HorizontalBar = 6,
    StackedBar = 7,
    GroupedBar = 14,
    FloatingBar = 15,

    // Line family
    Line = 2,
    Area = 3,
    SteppedLine = 16,
    MultiAxisLine = 17,

    // Circular family
    Pie = 4,
    Doughnut = 8,
    PolarArea = 18,

    // Radial
    Radar = 9,

    // Point-based
    Scatter = 5,
    Bubble = 19,

    // Mixed
    Composed = 11,

    // Plugin-based
    Treemap = 12,
    Funnel = 13,
}
