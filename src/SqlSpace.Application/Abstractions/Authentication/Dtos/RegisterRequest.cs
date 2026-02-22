using System.ComponentModel.DataAnnotations;

namespace SqlSpace.Application.Abstractions.Auth;

public class RegisterRequest
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(30)]
    public string Username { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Password { get; set; } = string.Empty;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

}
