using System.ComponentModel.DataAnnotations;

namespace NameMatch.Application.DTOs.Session;

public class JoinSessionRequest
{
    [Required(ErrorMessage = "Join code is required")]
    [StringLength(6, MinimumLength = 6, ErrorMessage = "Join code must be exactly 6 characters")]
    [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Join code must contain only uppercase letters and numbers")]
    public required string JoinCode { get; set; }
}
