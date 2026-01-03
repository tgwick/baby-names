using System.ComponentModel.DataAnnotations;

namespace NameMatch.Application.DTOs.Auth;

public class LoginRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256, ErrorMessage = "Email must not exceed 256 characters")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [StringLength(128, ErrorMessage = "Password must not exceed 128 characters")]
    public required string Password { get; set; }
}
