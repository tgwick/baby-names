using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Vote;

public class SubmitVoteRequest
{
    public int NameId { get; set; }
    public VoteType VoteType { get; set; }
}
