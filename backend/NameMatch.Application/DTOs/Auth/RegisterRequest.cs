using System.ComponentModel.DataAnnotations;

namespace NameMatch.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required, EmailAddress]
    public required string Email { get; set; }

    [Required, MinLength(8)]
    public required string Password { get; set; }

    public string? DisplayName { get; set; }
}
