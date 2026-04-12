namespace SqlSpace.Infrastructure.AI;

public class RagApiOptions
{
    public string BaseUrl { get; set; } = "http://localhost:8000";
    public string? InternalApiKey { get; set; }
    public int TimeoutSeconds { get; set; } = 60;
    public int MaxUploadSizeMb { get; set; } = 20;
    public int DefaultTopK { get; set; } = 5;
    public string AllowedExtensions { get; set; } = ".pdf,.docx,.txt";
}
