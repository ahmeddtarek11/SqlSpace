using System.ComponentModel.DataAnnotations;

namespace SqlSpace.Api.Controllers.SavedQueries.Dtos;

public sealed class RenameSavedQueryRequest
{
    [Required]
    public string Name { get; set; } = string.Empty;
}
