namespace NameMatch.Application.DTOs.Vote;

public class VoteResultDto
{
    public int VoteId { get; set; }
    public bool IsMatch { get; set; }
    public MatchDto? Match { get; set; }
}
