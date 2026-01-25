using Microsoft.EntityFrameworkCore;
using NameMatch.Application.DTOs.Filters;
using NameMatch.Application.Interfaces;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Data;

namespace NameMatch.Infrastructure.Services;

public class FilterService : IFilterService
{
    private readonly ApplicationDbContext _context;

    public FilterService(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<IEnumerable<FilterQuestionDto>> GetFilterQuestionsAsync()
    {
        var questions = new List<FilterQuestionDto>
        {
            new()
            {
                QuestionId = "name_style",
                QuestionText = "What kind of names are you looking for?",
                FilterType = "NAME_STYLE",
                Options = new List<FilterOptionDto>
                {
                    new()
                    {
                        OptionId = "trendy",
                        Label = "Trendy",
                        Description = "Names that have risen in popularity recently",
                        ExampleNames = new List<string> { "Luna", "Liam", "Olivia", "Noah" }
                    },
                    new()
                    {
                        OptionId = "classic",
                        Label = "Classic",
                        Description = "Traditional names that have been consistently popular over time",
                        ExampleNames = new List<string> { "Elizabeth", "James", "Catherine", "William" }
                    },
                    new()
                    {
                        OptionId = "unique",
                        Label = "Unique",
                        Description = "Uncommon names that stand out from the crowd",
                        ExampleNames = new List<string> { "Elowen", "Atticus", "Juniper", "Caspian" }
                    },
                    new()
                    {
                        OptionId = "no_pref",
                        Label = "No preference",
                        Description = "Show me all types of names"
                    }
                }
            },
            new()
            {
                QuestionId = "syllables",
                QuestionText = "How long should the name sound?",
                FilterType = "SYLLABLES",
                Options = new List<FilterOptionDto>
                {
                    new()
                    {
                        OptionId = "short",
                        Label = "Short & Punchy",
                        Description = "1-2 syllables, quick to say",
                        ExampleNames = new List<string> { "Max", "Grace", "Cole", "Kate" },
                        MinValue = 1,
                        MaxValue = 2
                    },
                    new()
                    {
                        OptionId = "medium",
                        Label = "Medium Length",
                        Description = "2-3 syllables, a nice balance",
                        ExampleNames = new List<string> { "Emily", "Oliver", "Sophia", "William" },
                        MinValue = 2,
                        MaxValue = 3
                    },
                    new()
                    {
                        OptionId = "long",
                        Label = "Flowing & Elegant",
                        Description = "3+ syllables, graceful and distinguished",
                        ExampleNames = new List<string> { "Isabella", "Alexander", "Penelope", "Sebastian" },
                        MinValue = 3,
                        MaxValue = 10
                    },
                    new()
                    {
                        OptionId = "no_pref",
                        Label = "No preference",
                        Description = "Show me names of any length",
                        MinValue = null,
                        MaxValue = null
                    }
                }
            }
        };

        return Task.FromResult<IEnumerable<FilterQuestionDto>>(questions);
    }

    public async Task<SessionFiltersStatusDto> SubmitFiltersAsync(string userId, Guid sessionId, SubmitFiltersRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(request);

        // Get specified session and verify user access
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                (s.InitiatorId == userId || s.PartnerId == userId));

        if (session == null)
        {
            throw new InvalidOperationException("Session not found or you don't have access.");
        }

        // Get all questions to map answers
        var questions = (await GetFilterQuestionsAsync()).ToDictionary(q => q.QuestionId);

        // Remove existing filters for this user/session
        var existingFilters = await _context.SessionFilters
            .Where(f => f.UserId == userId && f.SessionId == session.Id)
            .ToListAsync();

        _context.SessionFilters.RemoveRange(existingFilters);

        // Create new filter from answers
        var filter = new SessionFilter
        {
            UserId = userId,
            SessionId = session.Id,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var answer in request.Answers)
        {
            if (!questions.TryGetValue(answer.QuestionId, out var question))
            {
                continue;
            }

            var option = question.Options.FirstOrDefault(o => o.OptionId == answer.SelectedOptionId);
            if (option == null || answer.SelectedOptionId == "no_pref")
            {
                continue;
            }

            switch (question.FilterType)
            {
                case "NAME_STYLE":
                    filter.NameStyle = answer.SelectedOptionId switch
                    {
                        "trendy" => NameStyle.Trendy,
                        "classic" => NameStyle.Classic,
                        "unique" => NameStyle.Unique,
                        _ => NameStyle.None
                    };
                    break;

                case "SYLLABLES":
                    filter.MinSyllables = option.MinValue;
                    filter.MaxSyllables = option.MaxValue;
                    break;

                case "ENDING_SOUND":
                    if (option.AllowedValues != null && option.AllowedValues.Count > 0)
                    {
                        filter.AllowedEndingSounds = string.Join(",", option.AllowedValues);
                    }
                    break;
            }
        }

        await _context.SessionFilters.AddAsync(filter);

        // Update session filter completion tracking
        var isInitiator = session.InitiatorId == userId;
        if (isInitiator)
        {
            session.InitiatorFiltersCompletedAt = DateTime.UtcNow;
        }
        else
        {
            session.PartnerFiltersCompletedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return new SessionFiltersStatusDto
        {
            SessionId = session.Id,
            InitiatorCompleted = session.InitiatorFiltersCompletedAt.HasValue,
            PartnerCompleted = session.PartnerFiltersCompletedAt.HasValue,
            InitiatorCompletedAt = session.InitiatorFiltersCompletedAt,
            PartnerCompletedAt = session.PartnerFiltersCompletedAt
        };
    }

    public async Task<SessionFiltersStatusDto?> GetFiltersStatusAsync(string userId, Guid sessionId)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                (s.InitiatorId == userId || s.PartnerId == userId));

        if (session == null)
        {
            return null;
        }

        return new SessionFiltersStatusDto
        {
            SessionId = session.Id,
            InitiatorCompleted = session.InitiatorFiltersCompletedAt.HasValue,
            PartnerCompleted = session.PartnerFiltersCompletedAt.HasValue,
            InitiatorCompletedAt = session.InitiatorFiltersCompletedAt,
            PartnerCompletedAt = session.PartnerFiltersCompletedAt
        };
    }

    public async Task<UserFiltersDto?> GetUserFiltersAsync(string userId, Guid sessionId)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                s.Id == sessionId &&
                (s.InitiatorId == userId || s.PartnerId == userId));

        if (session == null)
        {
            return null;
        }

        var filter = await _context.SessionFilters
            .FirstOrDefaultAsync(f => f.UserId == userId && f.SessionId == sessionId);

        if (filter == null)
        {
            return null;
        }

        return new UserFiltersDto
        {
            NameStyle = filter.NameStyle,
            MinPopularityScore = filter.MinPopularityScore,
            MaxPopularityScore = filter.MaxPopularityScore,
            MinSyllables = filter.MinSyllables,
            MaxSyllables = filter.MaxSyllables,
            AllowedEndingSounds = filter.AllowedEndingSounds?.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            CreatedAt = filter.CreatedAt
        };
    }

    public async Task<CombinedFiltersDto?> GetCombinedSessionFiltersAsync(Guid sessionId)
    {
        var filters = await _context.SessionFilters
            .Where(f => f.SessionId == sessionId)
            .ToListAsync();

        if (filters.Count == 0)
        {
            return null;
        }

        // Combine filters using intersection logic (most restrictive)
        var combined = new CombinedFiltersDto();

        // Name style: if both have the same preference, use it; if different, use None
        var nameStyles = filters.Select(f => f.NameStyle).Where(s => s != NameStyle.None).ToList();
        if (nameStyles.Count == 1)
        {
            // Only one partner has a preference
            combined.NameStyle = nameStyles[0];
        }
        else if (nameStyles.Count > 1 && nameStyles.Distinct().Count() == 1)
        {
            // Both partners have the same preference
            combined.NameStyle = nameStyles[0];
        }
        // If different preferences, leave as None (allow all)

        // Popularity: use the strictest range (max of mins, min of maxes)
        var minPops = filters.Where(f => f.MinPopularityScore.HasValue).Select(f => f.MinPopularityScore!.Value).ToList();
        var maxPops = filters.Where(f => f.MaxPopularityScore.HasValue).Select(f => f.MaxPopularityScore!.Value).ToList();

        if (minPops.Count > 0)
        {
            combined.MinPopularityScore = minPops.Max();
        }
        if (maxPops.Count > 0)
        {
            combined.MaxPopularityScore = maxPops.Min();
        }

        // Syllables: use the strictest range
        var minSyls = filters.Where(f => f.MinSyllables.HasValue).Select(f => f.MinSyllables!.Value).ToList();
        var maxSyls = filters.Where(f => f.MaxSyllables.HasValue).Select(f => f.MaxSyllables!.Value).ToList();

        if (minSyls.Count > 0)
        {
            combined.MinSyllables = minSyls.Max();
        }
        if (maxSyls.Count > 0)
        {
            combined.MaxSyllables = maxSyls.Min();
        }

        // Ending sounds: intersection of allowed sounds, or union if one has no preference
        var soundFilters = filters
            .Where(f => !string.IsNullOrEmpty(f.AllowedEndingSounds))
            .Select(f => f.AllowedEndingSounds!.Split(',', StringSplitOptions.RemoveEmptyEntries).ToHashSet())
            .ToList();

        if (soundFilters.Count == 1)
        {
            // Only one partner has a preference, use theirs
            combined.AllowedEndingSounds = soundFilters[0].ToList();
        }
        else if (soundFilters.Count > 1)
        {
            // Both have preferences, use intersection
            var intersection = soundFilters[0];
            foreach (var sounds in soundFilters.Skip(1))
            {
                intersection.IntersectWith(sounds);
            }
            if (intersection.Count > 0)
            {
                combined.AllowedEndingSounds = intersection.ToList();
            }
        }

        return combined;
    }
}
