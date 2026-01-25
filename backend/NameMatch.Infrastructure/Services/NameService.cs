using Microsoft.EntityFrameworkCore;
using NameMatch.Application.DTOs.Filters;
using NameMatch.Application.DTOs.Name;
using NameMatch.Application.Interfaces;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Data;

namespace NameMatch.Infrastructure.Services;

public class NameService : INameService
{
    private readonly ApplicationDbContext _context;
    private readonly IFilterService _filterService;
    private readonly Random _random = new();

    // Weight calculation constants
    private const float MinWeight = 0.3f;
    private const float BaseWeightMin = 0.5f;
    private const float BaseWeightMax = 1.5f;
    private const float PreferenceBoostMultiplier = 0.25f; // -0.5 to +0.5 per user
    private const float MutualBonus = 0.2f;

    public NameService(ApplicationDbContext context, IFilterService filterService)
    {
        _context = context;
        _filterService = filterService;
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
        var namesQuery = _context.Names
            .Include(n => n.CategoryMappings)
            .AsQueryable();

        // Filter by target gender
        if (session.TargetGender != Gender.Neutral)
        {
            namesQuery = namesQuery.Where(n =>
                n.Gender == session.TargetGender || n.Gender == Gender.Neutral);
        }

        // Apply hard filters (popularity, syllables, ending sounds)
        var combinedFilters = await _filterService.GetCombinedSessionFiltersAsync(session.Id);
        if (combinedFilters != null && combinedFilters.HasFilters)
        {
            namesQuery = ApplyHardFilters(namesQuery, combinedFilters);
        }

        // Exclude already voted names
        if (votedNameIds.Count > 0)
        {
            namesQuery = namesQuery.Where(n => !votedNameIds.Contains(n.Id));
        }

        // Get both users' preferences for this session
        var partnerId = session.InitiatorId == userId ? session.PartnerId : session.InitiatorId;
        var userPreferences = await GetUserPreferencesAsync(userId, session.Id);
        var partnerPreferences = partnerId != null
            ? await GetUserPreferencesAsync(partnerId, session.Id)
            : new Dictionary<int, int>();

        // Get category IDs that either user wants to exclude (Avoid or Dislike)
        var excludedCategoryIds = userPreferences
            .Where(p => p.Value <= (int)PreferenceLevel.Dislike)
            .Select(p => p.Key)
            .Union(partnerPreferences
                .Where(p => p.Value <= (int)PreferenceLevel.Dislike)
                .Select(p => p.Key))
            .ToHashSet();

        // Filter out names with excluded categories
        if (excludedCategoryIds.Count > 0)
        {
            namesQuery = namesQuery.Where(n =>
                !n.CategoryMappings.Any(cm => excludedCategoryIds.Contains(cm.CategoryId)));
        }

        // Get unvoted names
        var unvotedNames = await namesQuery.ToListAsync();

        if (unvotedNames.Count == 0)
        {
            return null;
        }

        // Check if we have preferences and category mappings to use weighted selection
        var hasPreferences = userPreferences.Count > 0 || partnerPreferences.Count > 0;
        var hasCategoryMappings = unvotedNames.Any(n => n.CategoryMappings.Count > 0);

        Name selectedName;

        if (hasPreferences && hasCategoryMappings)
        {
            // Use preference-weighted selection
            selectedName = SelectWeightedByPreferences(
                unvotedNames,
                userPreferences,
                partnerPreferences);
        }
        else
        {
            // Fall back to popularity-weighted selection
            selectedName = SelectWeightedByPopularity(unvotedNames);
        }

        return new NameDto
        {
            Id = selectedName.Id,
            NameText = selectedName.NameText,
            Gender = (int)selectedName.Gender,
            PopularityScore = selectedName.PopularityScore,
            Origin = selectedName.Origin
        };
    }

    /// <summary>
    /// Applies hard filters to the name query, excluding names that don't match the criteria.
    /// </summary>
    private static IQueryable<Name> ApplyHardFilters(IQueryable<Name> query, CombinedFiltersDto filters)
    {
        // Name style filter based on trend analysis
        query = filters.NameStyle switch
        {
            // Trendy: names with rising trend and recent peak
            NameStyle.Trendy => query.Where(n =>
                n.TrendScore > 0.3f && n.PeakDecade >= 2000),

            // Classic: names with high stability that have been around for many decades
            NameStyle.Classic => query.Where(n =>
                n.StabilityScore >= 0.5f && n.DecadesPresent >= 4),

            // Unique: uncommon names with low popularity
            NameStyle.Unique => query.Where(n =>
                n.PopularityScore <= 30),

            // None: no filter
            _ => query
        };

        // Popularity filter (legacy support)
        if (filters.MinPopularityScore.HasValue)
        {
            query = query.Where(n => n.PopularityScore >= filters.MinPopularityScore.Value);
        }
        if (filters.MaxPopularityScore.HasValue)
        {
            query = query.Where(n => n.PopularityScore <= filters.MaxPopularityScore.Value);
        }

        // Syllable filter
        if (filters.MinSyllables.HasValue)
        {
            query = query.Where(n => n.SyllableCount == null || n.SyllableCount >= filters.MinSyllables.Value);
        }
        if (filters.MaxSyllables.HasValue)
        {
            query = query.Where(n => n.SyllableCount == null || n.SyllableCount <= filters.MaxSyllables.Value);
        }

        // Ending sound filter - use EndsWith to match patterns like "a" matching "ia", "ma", "na", etc.
        if (filters.AllowedEndingSounds != null && filters.AllowedEndingSounds.Count > 0)
        {
            var sounds = filters.AllowedEndingSounds;
            query = query.Where(n =>
                n.EndingSound == null ||
                sounds.Any(s => n.EndingSound.EndsWith(s)));
        }

        return query;
    }

    /// <summary>
    /// Gets user preferences as a dictionary of CategoryId -> PreferenceLevel value
    /// </summary>
    private async Task<Dictionary<int, int>> GetUserPreferencesAsync(string userId, Guid sessionId)
    {
        return await _context.UserPreferences
            .Where(p => p.UserId == userId && p.SessionId == sessionId)
            .ToDictionaryAsync(p => p.CategoryId, p => (int)p.Level);
    }

    /// <summary>
    /// Selects a name using weighted random selection based on user preferences.
    /// Names matching both partners' preferences get highest weights.
    /// All names remain in the pool with minimum weight of 0.3.
    /// </summary>
    private Name SelectWeightedByPreferences(
        List<Name> names,
        Dictionary<int, int> userPrefs,
        Dictionary<int, int> partnerPrefs)
    {
        var weights = new List<(Name name, float weight)>();

        foreach (var name in names)
        {
            // Calculate preference scores for each user (-2 to +2)
            var userScore = CalculatePreferenceScore(name, userPrefs);
            var partnerScore = CalculatePreferenceScore(name, partnerPrefs);

            // Base weight from popularity (0.5 to 1.5)
            var baseWeight = BaseWeightMin + (name.PopularityScore / 100f) * (BaseWeightMax - BaseWeightMin);

            // Preference boosts (-0.5 to +0.5 per user)
            var userBoost = userScore * PreferenceBoostMultiplier;
            var partnerBoost = partnerScore * PreferenceBoostMultiplier;

            // Mutual bonus if both partners like similar categories
            var mutualBonus = (userScore > 0 && partnerScore > 0) ? MutualBonus : 0f;

            // Final weight (minimum 0.3 so nothing is excluded)
            var finalWeight = Math.Max(MinWeight, baseWeight + userBoost + partnerBoost + mutualBonus);

            weights.Add((name, finalWeight));
        }

        return SelectWeightedRandom(weights);
    }

    /// <summary>
    /// Calculates a preference score for a name based on its categories and user preferences.
    /// Returns a value from -2 to +2 (average of matching category preference levels).
    /// Returns 0 if the name has no category mappings.
    /// </summary>
    private static float CalculatePreferenceScore(Name name, Dictionary<int, int> preferences)
    {
        if (name.CategoryMappings.Count == 0 || preferences.Count == 0)
        {
            return 0f;
        }

        var matchingScores = new List<int>();

        foreach (var mapping in name.CategoryMappings)
        {
            if (preferences.TryGetValue(mapping.CategoryId, out var level))
            {
                // Weight by confidence
                matchingScores.Add((int)(level * mapping.Confidence));
            }
        }

        if (matchingScores.Count == 0)
        {
            return 0f;
        }

        // Return average score
        return (float)matchingScores.Average();
    }

    /// <summary>
    /// Selects a name using weighted random selection based on popularity only.
    /// Used as fallback when no preferences or category mappings exist.
    /// </summary>
    private Name SelectWeightedByPopularity(List<Name> names)
    {
        // Sort by popularity descending
        var sortedNames = names.OrderByDescending(n => n.PopularityScore).ToList();

        // Use quadratic weighting to favor popular names
        var offset = GetWeightedRandomOffset(sortedNames.Count);
        return sortedNames[offset];
    }

    /// <summary>
    /// Performs weighted random selection from a list of (item, weight) tuples.
    /// </summary>
    private Name SelectWeightedRandom(List<(Name name, float weight)> weightedItems)
    {
        var totalWeight = weightedItems.Sum(w => w.weight);
        var randomValue = _random.NextDouble() * totalWeight;

        var cumulative = 0f;
        foreach (var (name, weight) in weightedItems)
        {
            cumulative += weight;
            if (randomValue <= cumulative)
            {
                return name;
            }
        }

        // Fallback to last item (shouldn't happen with proper weights)
        return weightedItems[^1].name;
    }

    public async Task<int> GetNameCountForSessionAsync(Guid sessionId)
    {
        var session = await _context.Sessions.FindAsync(sessionId);
        if (session == null)
        {
            return 0;
        }

        var namesQuery = _context.Names
            .Include(n => n.CategoryMappings)
            .AsQueryable();

        if (session.TargetGender != Gender.Neutral)
        {
            namesQuery = namesQuery.Where(n =>
                n.Gender == session.TargetGender || n.Gender == Gender.Neutral);
        }

        // Apply hard filters (popularity, syllables, ending sounds)
        var combinedFilters = await _filterService.GetCombinedSessionFiltersAsync(session.Id);
        if (combinedFilters != null && combinedFilters.HasFilters)
        {
            namesQuery = ApplyHardFilters(namesQuery, combinedFilters);
        }

        // Get both users' preferences and filter out excluded categories
        var userPreferences = await GetUserPreferencesAsync(session.InitiatorId, session.Id);
        var partnerPreferences = session.PartnerId != null
            ? await GetUserPreferencesAsync(session.PartnerId, session.Id)
            : new Dictionary<int, int>();

        var excludedCategoryIds = userPreferences
            .Where(p => p.Value <= (int)PreferenceLevel.Dislike)
            .Select(p => p.Key)
            .Union(partnerPreferences
                .Where(p => p.Value <= (int)PreferenceLevel.Dislike)
                .Select(p => p.Key))
            .ToHashSet();

        if (excludedCategoryIds.Count > 0)
        {
            namesQuery = namesQuery.Where(n =>
                !n.CategoryMappings.Any(cm => excludedCategoryIds.Contains(cm.CategoryId)));
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
