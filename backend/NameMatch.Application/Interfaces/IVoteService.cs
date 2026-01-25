using NameMatch.Application.DTOs.Vote;
using NameMatch.Domain.Enums;

namespace NameMatch.Application.Interfaces;

public interface IVoteService
{
    /// <summary>
    /// Submits a vote for a name in the specified session.
    /// Returns vote result including whether it's a match.
    /// </summary>
    Task<VoteResultDto> SubmitVoteAsync(string userId, Guid sessionId, int nameId, VoteType voteType);

    /// <summary>
    /// Gets all matches (mutual likes) for the specified session.
    /// </summary>
    Task<IEnumerable<MatchDto>> GetMatchesAsync(string userId, Guid sessionId);

    /// <summary>
    /// Gets the count of matches for the specified session.
    /// </summary>
    Task<int> GetMatchCountAsync(string userId, Guid sessionId);

    /// <summary>
    /// Gets all votes by the user in the specified session.
    /// </summary>
    Task<IEnumerable<VoteDto>> GetUserVotesAsync(string userId, Guid sessionId);

    /// <summary>
    /// Gets vote statistics for the specified session.
    /// </summary>
    Task<VoteStatsDto> GetVoteStatsAsync(string userId, Guid sessionId);

    /// <summary>
    /// Gets all conflicts for the specified session.
    /// A conflict is when one user likes a name and the other dislikes it.
    /// </summary>
    Task<IEnumerable<ConflictDto>> GetConflictsAsync(string userId, Guid sessionId);

    /// <summary>
    /// Clears a user's dislike on a name in the specified session.
    /// The name will return to the voting pool for the user who cleared their dislike.
    /// </summary>
    Task<bool> ClearDislikeAsync(string userId, Guid sessionId, int nameId);
}
