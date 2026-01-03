using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Vote;

public class VoteDto
{
    public int Id { get; set; }
    public int NameId { get; set; }
    public string NameText { get; set; } = string.Empty;
    public VoteType VoteType { get; set; }
    public DateTime VotedAt { get; set; }
}
