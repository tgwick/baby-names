using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Filters;

public class UserFiltersDto
{
    public NameStyle NameStyle { get; set; } = NameStyle.None;
    public int? MinPopularityScore { get; set; }
    public int? MaxPopularityScore { get; set; }
    public int? MinSyllables { get; set; }
    public int? MaxSyllables { get; set; }
    public List<string>? AllowedEndingSounds { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Combined filters from both partners in a session.
/// Uses intersection logic (most restrictive of both).
/// </summary>
public class CombinedFiltersDto
{
    /// <summary>
    /// Combined name style preference. If both partners have the same style,
    /// use that. Otherwise, use None (allow all).
    /// </summary>
    public NameStyle NameStyle { get; set; } = NameStyle.None;
    public int? MinPopularityScore { get; set; }
    public int? MaxPopularityScore { get; set; }
    public int? MinSyllables { get; set; }
    public int? MaxSyllables { get; set; }
    public List<string>? AllowedEndingSounds { get; set; }

    /// <summary>
    /// True if there are any active filters to apply.
    /// </summary>
    public bool HasFilters =>
        NameStyle != NameStyle.None ||
        MinPopularityScore.HasValue ||
        MaxPopularityScore.HasValue ||
        MinSyllables.HasValue ||
        MaxSyllables.HasValue ||
        (AllowedEndingSounds != null && AllowedEndingSounds.Count > 0);
}
