using NameMatch.Domain.Enums;

namespace NameMatch.Domain.Entities;

/// <summary>
/// Stores hard filter settings for a user within a session.
/// These filters exclude names entirely (unlike soft preferences which weight names).
/// </summary>
public class SessionFilter
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public Guid SessionId { get; set; }

    // Name style filter (Trendy, Classic, Unique)
    public NameStyle NameStyle { get; set; } = NameStyle.None;

    // Popularity filter (1-100 scale) - kept for backward compatibility
    public int? MinPopularityScore { get; set; }
    public int? MaxPopularityScore { get; set; }

    // Syllable count filter
    public int? MinSyllables { get; set; }
    public int? MaxSyllables { get; set; }

    // Ending sounds filter (comma-separated list, e.g., "a,ia,n,en")
    public string? AllowedEndingSounds { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public Session Session { get; set; } = null!;
}
