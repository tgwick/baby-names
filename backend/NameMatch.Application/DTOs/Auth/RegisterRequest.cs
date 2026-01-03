using System.ComponentModel.DataAnnotations;

namespace NameMatch.Application.DTOs.Auth;

public class RegisterRequest
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email address")]
    [StringLength(256, ErrorMessage = "Email must not exceed 256 characters")]
    public required string Email { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [MinLength(8, ErrorMessage = "Password must be at least 8 characters")]
    [StringLength(128, ErrorMessage = "Password must not exceed 128 characters")]
    public required string Password { get; set; }

    [StringLength(100, ErrorMessage = "Display name must not exceed 100 characters")]
    public string? DisplayName { get; set; }
}
