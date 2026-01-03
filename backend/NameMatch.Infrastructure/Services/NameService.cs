using Microsoft.EntityFrameworkCore;
using NameMatch.Application.DTOs.Name;
using NameMatch.Application.Interfaces;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Data;

namespace NameMatch.Infrastructure.Services;

public class NameService : INameService
{
    private readonly ApplicationDbContext _context;
    private readonly Random _random = new();

    public NameService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<NameDto?> GetNextUnvotedNameAsync(string userId)
    {
        // Find the user's active session
        var session = await _context.Sessions
            .Where(s => (s.InitiatorId == userId || s.PartnerId == userId) &&
                        s.Status == SessionStatus.Active)
            .FirstOrDefaultAsync();

        if (session == null)
        {
            return null;
        }

        // Get IDs of names this user has already voted on in this session
        var votedNameIds = await _context.Votes
            .Where(v => v.UserId == userId && v.SessionId == session.Id)
            .Select(v => v.NameId)
            .ToListAsync();

        // Build query for unvoted names matching session's target gender
        var namesQuery = _context.Names.AsQueryable();

        // Filter by target gender
        if (session.TargetGender != Gender.Neutral)
        {
            // Include names of the target gender OR neutral names
            namesQuery = namesQuery.Where(n =>
                n.Gender == session.TargetGender || n.Gender == Gender.Neutral);
        }

        // Exclude already voted names
        if (votedNameIds.Count > 0)
        {
            namesQuery = namesQuery.Where(n => !votedNameIds.Contains(n.Id));
        }

        // Get count for random selection
        var count = await namesQuery.CountAsync();
        if (count == 0)
        {
            return null;
        }

        // Select a random name with weighted probability toward popular names
        // Use skip/take with random offset, weighted by popularity
        var offset = GetWeightedRandomOffset(count);
        var name = await namesQuery
            .OrderByDescending(n => n.PopularityScore)
            .Skip(offset)
            .Take(1)
            .FirstOrDefaultAsync();

        if (name == null)
        {
            return null;
        }

        return new NameDto
        {
            Id = name.Id,
            NameText = name.NameText,
            Gender = (int)name.Gender,
            PopularityScore = name.PopularityScore,
            Origin = name.Origin
        };
    }

    public async Task<int> GetNameCountForSessionAsync(Guid sessionId)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
        {
            return 0;
        }

        var namesQuery = _context.Names.AsQueryable();

        if (session.TargetGender != Gender.Neutral)
        {
            namesQuery = namesQuery.Where(n =>
                n.Gender == session.TargetGender || n.Gender == Gender.Neutral);
        }

        return await namesQuery.CountAsync();
    }

    /// <summary>
    /// Returns a weighted random offset that favors lower values (more popular names).
    /// Uses inverse square root weighting to prefer popular names while still allowing discovery.
    /// </summary>
    private int GetWeightedRandomOffset(int count)
    {
        // Use a distribution that favors lower offsets (more popular names)
        // Formula: offset = floor(count * (random^2))
        // This creates a distribution where 50% of picks are in the top 25%
        var r = _random.NextDouble();
        var weighted = r * r; // Square gives quadratic weighting toward lower values
        return (int)(count * weighted);
    }
}
