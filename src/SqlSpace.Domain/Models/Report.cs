namespace SqlSpace.Domain.Models;

public class Report
{
    public Guid ReportId { get; set; }
    public Guid ConnectionId { get; set; }
    public string UserId { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;
    public string OriginalPrompt { get; set; } = string.Empty;
    public string? Summary { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public ConnectedDatabase DatabaseConnection { get; set; } = null!;
    public ICollection<ReportSection> Sections { get; set; } = new List<ReportSection>();
}
