using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Vote;

public class ConflictDto
{
    public int NameId { get; set; }
    public string NameText { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string? Origin { get; set; }
    public int PopularityScore { get; set; }

    /// <summary>
    /// True if I liked this name but partner disliked it.
    /// False if partner liked it but I disliked it.
    /// </summary>
    public bool ILikedIt { get; set; }

    /// <summary>
    /// When the conflict was created (when the second vote came in)
    /// </summary>
    public DateTime ConflictedAt { get; set; }
}
