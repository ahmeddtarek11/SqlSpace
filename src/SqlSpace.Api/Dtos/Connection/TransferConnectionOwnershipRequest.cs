using System.ComponentModel.DataAnnotations;

namespace SqlSpace.Api.Controllers.ConnectionManagement.Dtos;

public sealed class TransferConnectionOwnershipRequest
{
    [Required]
    [EmailAddress]
    public string NewAdminEmail { get; set; } = string.Empty;
}
