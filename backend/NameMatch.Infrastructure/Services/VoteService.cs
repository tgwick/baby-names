using Microsoft.EntityFrameworkCore;
using NameMatch.Application.DTOs.Vote;
using NameMatch.Application.Interfaces;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Data;

namespace NameMatch.Infrastructure.Services;

public class VoteService : IVoteService
{
    private readonly ApplicationDbContext _context;

    public VoteService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<VoteResultDto> SubmitVoteAsync(string userId, int nameId, VoteType voteType)
    {
        // Find the user's active session
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                (s.InitiatorId == userId || s.PartnerId == userId) &&
                s.Status == SessionStatus.Active);

        if (session == null)
        {
            throw new InvalidOperationException("You must have an active session to vote.");
        }

        // Verify the name exists
        var name = await _context.Names.FindAsync(nameId);
        if (name == null)
        {
            throw new InvalidOperationException("Name not found.");
        }

        // Check for existing vote (to update or prevent duplicate)
        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(v =>
                v.UserId == userId &&
                v.NameId == nameId &&
                v.SessionId == session.Id);

        Vote vote;
        if (existingVote != null)
        {
            // Update existing vote
            existingVote.VoteType = voteType;
            existingVote.VotedAt = DateTime.UtcNow;
            vote = existingVote;
        }
        else
        {
            // Create new vote
            vote = new Vote
            {
                UserId = userId,
                NameId = nameId,
                SessionId = session.Id,
                VoteType = voteType,
                VotedAt = DateTime.UtcNow
            };
            _context.Votes.Add(vote);
        }

        await _context.SaveChangesAsync();

        // Check if this creates a match (both users liked the same name)
        var result = new VoteResultDto
        {
            VoteId = vote.Id,
            IsMatch = false
        };

        if (voteType == VoteType.Like)
        {
            var isMatch = await CheckForMatchAsync(session, userId, nameId);
            if (isMatch)
            {
                result.IsMatch = true;
                result.Match = new MatchDto
                {
                    NameId = name.Id,
                    NameText = name.NameText,
                    Gender = name.Gender,
                    Origin = name.Origin,
                    PopularityScore = name.PopularityScore,
                    MatchedAt = DateTime.UtcNow
                };
            }
        }

        return result;
    }

    public async Task<IEnumerable<MatchDto>> GetMatchesAsync(string userId)
    {
        var session = await GetActiveSessionAsync(userId);
        if (session == null)
        {
            return [];
        }

        var partnerId = session.InitiatorId == userId ? session.PartnerId : session.InitiatorId;
        if (partnerId == null)
        {
            return [];
        }

        // Find names where both users have Like votes
        var matches = await (
            from userVote in _context.Votes
            join partnerVote in _context.Votes
                on new { userVote.NameId, userVote.SessionId }
                equals new { partnerVote.NameId, partnerVote.SessionId }
            join name in _context.Names on userVote.NameId equals name.Id
            where userVote.UserId == userId
                && partnerVote.UserId == partnerId
                && userVote.SessionId == session.Id
                && userVote.VoteType == VoteType.Like
                && partnerVote.VoteType == VoteType.Like
            orderby userVote.VotedAt > partnerVote.VotedAt
                ? userVote.VotedAt
                : partnerVote.VotedAt descending
            select new MatchDto
            {
                NameId = name.Id,
                NameText = name.NameText,
                Gender = name.Gender,
                Origin = name.Origin,
                PopularityScore = name.PopularityScore,
                MatchedAt = userVote.VotedAt > partnerVote.VotedAt
                    ? userVote.VotedAt
                    : partnerVote.VotedAt
            }
        ).ToListAsync();

        return matches;
    }

    public async Task<int> GetMatchCountAsync(string userId)
    {
        var session = await GetActiveSessionAsync(userId);
        if (session == null)
        {
            return 0;
        }

        var partnerId = session.InitiatorId == userId ? session.PartnerId : session.InitiatorId;
        if (partnerId == null)
        {
            return 0;
        }

        // Count names where both users have Like votes
        var matchCount = await (
            from userVote in _context.Votes
            join partnerVote in _context.Votes
                on new { userVote.NameId, userVote.SessionId }
                equals new { partnerVote.NameId, partnerVote.SessionId }
            where userVote.UserId == userId
                && partnerVote.UserId == partnerId
                && userVote.SessionId == session.Id
                && userVote.VoteType == VoteType.Like
                && partnerVote.VoteType == VoteType.Like
            select userVote.Id
        ).CountAsync();

        return matchCount;
    }

    public async Task<IEnumerable<VoteDto>> GetUserVotesAsync(string userId)
    {
        var session = await GetActiveSessionAsync(userId);
        if (session == null)
        {
            return [];
        }

        var votes = await _context.Votes
            .Include(v => v.Name)
            .Where(v => v.UserId == userId && v.SessionId == session.Id)
            .OrderByDescending(v => v.VotedAt)
            .Select(v => new VoteDto
            {
                Id = v.Id,
                NameId = v.NameId,
                NameText = v.Name.NameText,
                VoteType = v.VoteType,
                VotedAt = v.VotedAt
            })
            .ToListAsync();

        return votes;
    }

    public async Task<VoteStatsDto> GetVoteStatsAsync(string userId)
    {
        var session = await GetActiveSessionAsync(userId);
        if (session == null)
        {
            return new VoteStatsDto();
        }

        var votes = await _context.Votes
            .Where(v => v.UserId == userId && v.SessionId == session.Id)
            .ToListAsync();

        var totalLikes = votes.Count(v => v.VoteType == VoteType.Like);
        var totalDislikes = votes.Count(v => v.VoteType == VoteType.Dislike);
        var matchCount = await GetMatchCountAsync(userId);

        // Calculate remaining names
        var votedNameIds = votes.Select(v => v.NameId).ToList();
        var namesQuery = _context.Names.AsQueryable();

        if (session.TargetGender != Gender.Neutral)
        {
            namesQuery = namesQuery.Where(n =>
                n.Gender == session.TargetGender || n.Gender == Gender.Neutral);
        }

        var totalNames = await namesQuery.CountAsync();
        var namesRemaining = totalNames - votes.Count;

        return new VoteStatsDto
        {
            TotalVotes = votes.Count,
            LikeCount = totalLikes,
            DislikeCount = totalDislikes,
            MatchCount = matchCount,
            NamesRemaining = Math.Max(0, namesRemaining)
        };
    }

    private async Task<Session?> GetActiveSessionAsync(string userId)
    {
        return await _context.Sessions
            .FirstOrDefaultAsync(s =>
                (s.InitiatorId == userId || s.PartnerId == userId) &&
                s.Status == SessionStatus.Active);
    }

    private async Task<bool> CheckForMatchAsync(Session session, string currentUserId, int nameId)
    {
        var partnerId = session.InitiatorId == currentUserId
            ? session.PartnerId
            : session.InitiatorId;

        if (partnerId == null)
        {
            return false;
        }

        // Check if partner has also liked this name
        var partnerVote = await _context.Votes
            .FirstOrDefaultAsync(v =>
                v.UserId == partnerId &&
                v.NameId == nameId &&
                v.SessionId == session.Id &&
                v.VoteType == VoteType.Like);

        return partnerVote != null;
    }
}
