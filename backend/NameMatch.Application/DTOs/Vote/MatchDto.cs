using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Vote;

public class MatchDto
{
    public int NameId { get; set; }
    public string NameText { get; set; } = string.Empty;
    public Gender Gender { get; set; }
    public string? Origin { get; set; }
    public int PopularityScore { get; set; }
    public DateTime MatchedAt { get; set; }
}
