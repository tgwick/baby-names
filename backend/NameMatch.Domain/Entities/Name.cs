using NameMatch.Domain.Enums;

namespace NameMatch.Domain.Entities;

public class Name
{
    public int Id { get; set; }
    public required string NameText { get; set; }
    public Gender Gender { get; set; }
    public int PopularityScore { get; set; }
    public string? Origin { get; set; }

    // Enrichment fields
    public string? Meaning { get; set; }
    public int? SyllableCount { get; set; }
    public string? EndingSound { get; set; }
    public string? SoundType { get; set; }

    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<NameCategoryMapping> CategoryMappings { get; set; } = new List<NameCategoryMapping>();
}
