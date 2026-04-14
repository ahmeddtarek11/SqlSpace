namespace SqlSpace.Application.DTOs.Reports;

public class ReportDto
{
    public Guid ReportId { get; set; }
    public Guid ConnectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalPrompt { get; set; } = string.Empty;
    public string? Summary { get; set; }
    public IReadOnlyList<ReportSectionDto> Sections { get; set; } = [];
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// Lightweight header used in list views — no sections body.
/// </summary>
public class ReportHeaderDto
{
    public Guid ReportId { get; set; }
    public Guid ConnectionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OriginalPrompt { get; set; } = string.Empty;
    public int SectionCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
