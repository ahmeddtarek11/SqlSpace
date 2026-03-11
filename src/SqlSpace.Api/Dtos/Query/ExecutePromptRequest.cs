using System.ComponentModel.DataAnnotations;

namespace SqlSpace.Api.Controllers.Query.Dtos;

public sealed class ExecutePromptRequest
{
    [Required]
    public Guid ConnectionId { get; set; }

    [Required]
    public string UserPrompt { get; set; } = string.Empty;
}
