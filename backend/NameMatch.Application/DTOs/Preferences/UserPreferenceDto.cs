using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Preferences;

public class UserPreferenceDto
{
    public int Id { get; set; }
    public int CategoryId { get; set; }
    public required string CategoryCode { get; set; }
    public required string CategoryName { get; set; }
    public required string CategoryType { get; set; }
    public PreferenceLevel Level { get; set; }
}
