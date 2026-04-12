using SqlSpace.Domain.Enums;

namespace SqlSpace.Domain.Models;


public class KnowledgeDocument
{
    public Guid DocumentId { get; set; }

    /// <summary>FK to ConnectedDatabase — scopes this document to a connection.</summary>
    public Guid ConnectionId { get; set; }

    public string UploadedByUserId { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    /// <summary>"document" for user uploads, "db_context" for auto-ingested schema.</summary>
    public string SourceType { get; set; } = "document";

    /// <summary>Pending → Processing → Indexed | Failed</summary>
    public KnowledgeDocumentStatus Status { get; set; } = KnowledgeDocumentStatus.Pending;

    /// <summary>The file_id returned by the Python RAG service. Used for querying/deleting.</summary>
    public string? PythonFileId { get; set; }

    public int ChunksCreated { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }

    public bool IsDeleted { get; set; }

    // Navigation
    public ConnectedDatabase DatabaseConnection { get; set; } = null!;
}
