using NameMatch.Application.DTOs.Vote;
using NameMatch.Domain.Enums;

namespace NameMatch.Application.Interfaces;

public interface IVoteService
{
    /// <summary>
    /// Submits a vote for a name in the user's current session.
    /// Returns vote result including whether it's a match.
    /// </summary>
    Task<VoteResultDto> SubmitVoteAsync(string userId, int nameId, VoteType voteType);

    /// <summary>
    /// Gets all matches (mutual likes) for the user's current session.
    /// </summary>
    Task<IEnumerable<MatchDto>> GetMatchesAsync(string userId);

    /// <summary>
    /// Gets the count of matches for the user's current session.
    /// </summary>
    Task<int> GetMatchCountAsync(string userId);

    /// <summary>
    /// Gets all votes by the user in their current session.
    /// </summary>
    Task<IEnumerable<VoteDto>> GetUserVotesAsync(string userId);

    /// <summary>
    /// Gets vote statistics for the user's current session.
    /// </summary>
    Task<VoteStatsDto> GetVoteStatsAsync(string userId);

    /// <summary>
    /// Gets all conflicts for the user's current session.
    /// A conflict is when one user likes a name and the other dislikes it.
    /// </summary>
    Task<IEnumerable<ConflictDto>> GetConflictsAsync(string userId);

    /// <summary>
    /// Clears a user's dislike on a name, removing it from conflicts.
    /// The name will return to the voting pool for the user who cleared their dislike.
    /// </summary>
    Task<bool> ClearDislikeAsync(string userId, int nameId);
}
