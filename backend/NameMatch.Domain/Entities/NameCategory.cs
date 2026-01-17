namespace NameMatch.Domain.Entities;

public class NameCategory
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string DisplayName { get; set; }
    public required string CategoryType { get; set; } // STYLE, ORIGIN, SOUND
    public string? Description { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<NameCategoryMapping> NameMappings { get; set; } = new List<NameCategoryMapping>();
    public ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();
}
