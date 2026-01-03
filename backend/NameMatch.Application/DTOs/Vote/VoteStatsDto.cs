namespace NameMatch.Application.DTOs.Vote;

public class VoteStatsDto
{
    public int TotalVotes { get; set; }
    public int LikeCount { get; set; }
    public int DislikeCount { get; set; }
    public int MatchCount { get; set; }
    public int NamesRemaining { get; set; }
}
