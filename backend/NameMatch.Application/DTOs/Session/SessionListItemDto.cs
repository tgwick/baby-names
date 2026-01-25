using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Session;

public class SessionListItemDto
{
    public Guid Id { get; set; }
    public string? PartnerDisplayName { get; set; }
    public DateTime CreatedAt { get; set; }
    public SessionStatus Status { get; set; }
    public bool IsArchived { get; set; }
    public Gender TargetGender { get; set; }
    public int MatchCount { get; set; }
    public int VoteCount { get; set; }
    public bool IsInitiator { get; set; }
}
