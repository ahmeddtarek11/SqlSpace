using System.ComponentModel.DataAnnotations;

namespace SqlSpace.Application.Abstractions.SavedQueries;

public sealed class CreateSavedQueryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;

    [Required]
    public Guid QueryHistoryId { get; set; }
}
