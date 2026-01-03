using NameMatch.Domain.Enums;

namespace NameMatch.Domain.Entities;

public class Vote
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public int NameId { get; set; }
    public Guid SessionId { get; set; }
    public VoteType VoteType { get; set; }
    public DateTime VotedAt { get; set; }

    public Name Name { get; set; } = null!;
    public Session Session { get; set; } = null!;
}
