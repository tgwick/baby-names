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

    // Trend analysis fields (from SSA historical data)
    /// <summary>
    /// Trend score from -1 (declining) to +1 (rising).
    /// Compares recent popularity to historical popularity.
    /// </summary>
    public float? TrendScore { get; set; }

    /// <summary>
    /// Stability score from 0 (volatile) to 1 (consistent).
    /// Measures how steady the name's popularity has been over time.
    /// </summary>
    public float? StabilityScore { get; set; }

    /// <summary>
    /// The decade when the name was most popular (e.g., 1980, 2000).
    /// </summary>
    public int? PeakDecade { get; set; }

    /// <summary>
    /// Number of decades the name appears in the SSA data.
    /// Higher values indicate longer-established names.
    /// </summary>
    public int? DecadesPresent { get; set; }

    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<NameCategoryMapping> CategoryMappings { get; set; } = new List<NameCategoryMapping>();
}
