using System.ComponentModel.DataAnnotations;

namespace SqlSpace.Api.Controllers.ConnectionManagement.Dtos;

public sealed class UpdateConnectionPasswordRequest
{
    [Required]
    public string NewPassword { get; set; } = string.Empty;
}
