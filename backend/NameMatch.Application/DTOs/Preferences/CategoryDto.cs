namespace NameMatch.Application.DTOs.Preferences;

public class CategoryDto
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string DisplayName { get; set; }
    public required string CategoryType { get; set; }
    public string? Description { get; set; }
}
