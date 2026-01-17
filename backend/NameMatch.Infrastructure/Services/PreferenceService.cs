using Microsoft.EntityFrameworkCore;
using NameMatch.Application.DTOs.Preferences;
using NameMatch.Application.Interfaces;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Data;

namespace NameMatch.Infrastructure.Services;

public class PreferenceService : IPreferenceService
{
    private readonly ApplicationDbContext _context;

    public PreferenceService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<CategoryDto>> GetCategoriesAsync(string? categoryType = null)
    {
        var query = _context.NameCategories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(categoryType))
        {
            query = query.Where(c => c.CategoryType == categoryType.ToUpper());
        }

        var categories = await query
            .OrderBy(c => c.DisplayOrder)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Code = c.Code,
                DisplayName = c.DisplayName,
                CategoryType = c.CategoryType,
                Description = c.Description
            })
            .ToListAsync();

        return categories;
    }

    public Task<IEnumerable<PreferenceQuestionDto>> GetQuestionsAsync()
    {
        // Predefined questionnaire - questions map to categories
        var questions = new List<PreferenceQuestionDto>
        {
            new()
            {
                QuestionId = "style",
                QuestionText = "What name styles appeal to you most?",
                CategoryType = "STYLE",
                AllowMultiple = true,
                Options = new List<PreferenceOptionDto>
                {
                    new() { OptionId = "classic", Label = "Classic & Traditional", Description = "Timeless names that never go out of style", CategoryCodes = new List<string> { "CLASSIC" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "modern", Label = "Modern & Contemporary", Description = "Fresh, current-feeling names", CategoryCodes = new List<string> { "MODERN" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "unique", Label = "Unique & Rare", Description = "Names that stand out from the crowd", CategoryCodes = new List<string> { "UNIQUE" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "no_pref", Label = "No preference", Description = "Open to all styles", CategoryCodes = new List<string>(), PreferenceLevel = (int)PreferenceLevel.Neutral }
                }
            },
            new()
            {
                QuestionId = "origin",
                QuestionText = "Do you have any cultural or origin preferences?",
                CategoryType = "ORIGIN",
                AllowMultiple = true,
                Options = new List<PreferenceOptionDto>
                {
                    new() { OptionId = "hebrew", Label = "Hebrew", Description = "Names with Hebrew/Biblical origins", CategoryCodes = new List<string> { "HEBREW" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "latin", Label = "Latin/Romance", Description = "Names from Latin or Romance languages", CategoryCodes = new List<string> { "LATIN" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "celtic", Label = "Celtic", Description = "Irish, Scottish, Welsh traditions", CategoryCodes = new List<string> { "CELTIC" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "greek", Label = "Greek", Description = "Names with Greek origins", CategoryCodes = new List<string> { "GREEK" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "no_pref", Label = "No preference", Description = "Open to all origins", CategoryCodes = new List<string>(), PreferenceLevel = (int)PreferenceLevel.Neutral }
                }
            },
            new()
            {
                QuestionId = "length",
                QuestionText = "Do you prefer shorter or longer names?",
                CategoryType = "SOUND",
                AllowMultiple = false,
                Options = new List<PreferenceOptionDto>
                {
                    new() { OptionId = "short", Label = "Short & Punchy", Description = "1-2 syllables (e.g., Max, Kate)", CategoryCodes = new List<string> { "SHORT" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "long", Label = "Flowing & Elegant", Description = "3+ syllables (e.g., Alexander, Isabella)", CategoryCodes = new List<string> { "LONG" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "no_pref", Label = "No preference", Description = "Open to any length", CategoryCodes = new List<string>(), PreferenceLevel = (int)PreferenceLevel.Neutral }
                }
            },
            new()
            {
                QuestionId = "sound",
                QuestionText = "What kind of sound do you prefer?",
                CategoryType = "SOUND",
                AllowMultiple = false,
                Options = new List<PreferenceOptionDto>
                {
                    new() { OptionId = "soft", Label = "Soft & Gentle", Description = "Names with soft consonants (e.g., Lily, Emma)", CategoryCodes = new List<string> { "SOFT" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "strong", Label = "Strong & Bold", Description = "Names with strong consonants (e.g., Jack, Victor)", CategoryCodes = new List<string> { "STRONG" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "no_pref", Label = "No preference", Description = "Open to all sounds", CategoryCodes = new List<string>(), PreferenceLevel = (int)PreferenceLevel.Neutral }
                }
            },
            new()
            {
                QuestionId = "biblical",
                QuestionText = "How do you feel about Biblical/religious names?",
                CategoryType = "STYLE",
                AllowMultiple = false,
                Options = new List<PreferenceOptionDto>
                {
                    new() { OptionId = "love", Label = "Love them", Description = "Show me more Biblical names", CategoryCodes = new List<string> { "BIBLICAL" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "like", Label = "They're fine", Description = "Include them in the mix", CategoryCodes = new List<string> { "BIBLICAL" }, PreferenceLevel = (int)PreferenceLevel.Neutral },
                    new() { OptionId = "avoid", Label = "Do not include", Description = "Exclude Biblical names", CategoryCodes = new List<string> { "BIBLICAL" }, PreferenceLevel = (int)PreferenceLevel.Avoid }
                }
            },
            new()
            {
                QuestionId = "nature",
                QuestionText = "How do you feel about nature-inspired names?",
                CategoryType = "STYLE",
                AllowMultiple = false,
                Options = new List<PreferenceOptionDto>
                {
                    new() { OptionId = "love", Label = "Love them", Description = "Names like River, Rose, Sky", CategoryCodes = new List<string> { "NATURE" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "like", Label = "They're fine", Description = "Include them in the mix", CategoryCodes = new List<string> { "NATURE" }, PreferenceLevel = (int)PreferenceLevel.Neutral },
                    new() { OptionId = "avoid", Label = "Do not include", Description = "Exclude nature names", CategoryCodes = new List<string> { "NATURE" }, PreferenceLevel = (int)PreferenceLevel.Avoid }
                }
            },
            new()
            {
                QuestionId = "trendy",
                QuestionText = "Trendy or timeless?",
                CategoryType = "STYLE",
                AllowMultiple = false,
                Options = new List<PreferenceOptionDto>
                {
                    new() { OptionId = "trendy", Label = "Trendy & Popular", Description = "Currently popular names", CategoryCodes = new List<string> { "TRENDY" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "vintage", Label = "Vintage & Retro", Description = "Old-fashioned names making a comeback", CategoryCodes = new List<string> { "VINTAGE" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "classic", Label = "Classic & Timeless", Description = "Names that never go out of style", CategoryCodes = new List<string> { "CLASSIC" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "no_pref", Label = "No preference", Description = "Open to all", CategoryCodes = new List<string>(), PreferenceLevel = (int)PreferenceLevel.Neutral }
                }
            }
        };

        return Task.FromResult<IEnumerable<PreferenceQuestionDto>>(questions);
    }

    public async Task<SessionPreferencesStatusDto> SubmitPreferencesAsync(string userId, SubmitPreferencesRequest request)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(request);

        // Get user's active or waiting session (allow preferences before partner joins)
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                (s.InitiatorId == userId || s.PartnerId == userId) &&
                (s.Status == SessionStatus.Active || s.Status == SessionStatus.WaitingForPartner));

        if (session == null)
        {
            throw new InvalidOperationException("No active session found. Join or create a session first.");
        }

        // Get all questions to map answers to categories
        var questions = (await GetQuestionsAsync()).ToDictionary(q => q.QuestionId);

        // Get all categories for lookup
        var categories = await _context.NameCategories.ToDictionaryAsync(c => c.Code, c => c.Id);

        // Remove existing preferences for this user/session
        var existingPrefs = await _context.UserPreferences
            .Where(p => p.UserId == userId && p.SessionId == session.Id)
            .ToListAsync();

        _context.UserPreferences.RemoveRange(existingPrefs);

        // Process each answer and create preferences
        var newPreferences = new List<UserPreference>();

        foreach (var answer in request.Answers)
        {
            if (!questions.TryGetValue(answer.QuestionId, out var question))
            {
                continue; // Skip unknown questions
            }

            foreach (var optionId in answer.SelectedOptionIds)
            {
                var option = question.Options.FirstOrDefault(o => o.OptionId == optionId);
                if (option == null) continue;

                foreach (var categoryCode in option.CategoryCodes)
                {
                    if (categories.TryGetValue(categoryCode, out var categoryId))
                    {
                        // Check if we already have a preference for this category
                        var existing = newPreferences.FirstOrDefault(p => p.CategoryId == categoryId);
                        if (existing == null)
                        {
                            newPreferences.Add(new UserPreference
                            {
                                UserId = userId,
                                SessionId = session.Id,
                                CategoryId = categoryId,
                                Level = (PreferenceLevel)option.PreferenceLevel,
                                CreatedAt = DateTime.UtcNow
                            });
                        }
                    }
                }
            }
        }

        await _context.UserPreferences.AddRangeAsync(newPreferences);

        // Update session setup status
        var isInitiator = session.InitiatorId == userId;
        if (isInitiator)
        {
            session.InitiatorPrefsCompletedAt = DateTime.UtcNow;
        }
        else
        {
            session.PartnerPrefsCompletedAt = DateTime.UtcNow;
        }

        // Update setup status based on who has completed
        if (session.InitiatorPrefsCompletedAt.HasValue && session.PartnerPrefsCompletedAt.HasValue)
        {
            session.SetupStatus = SessionSetupStatus.Ready;
        }
        else if (session.InitiatorPrefsCompletedAt.HasValue)
        {
            session.SetupStatus = SessionSetupStatus.PendingPartnerPreferences;
        }
        else
        {
            session.SetupStatus = SessionSetupStatus.PendingInitiatorPreferences;
        }

        await _context.SaveChangesAsync();

        return new SessionPreferencesStatusDto
        {
            SessionId = session.Id,
            SetupStatus = session.SetupStatus,
            InitiatorCompleted = session.InitiatorPrefsCompletedAt.HasValue,
            PartnerCompleted = session.PartnerPrefsCompletedAt.HasValue,
            InitiatorCompletedAt = session.InitiatorPrefsCompletedAt,
            PartnerCompletedAt = session.PartnerPrefsCompletedAt
        };
    }

    public async Task<SessionPreferencesStatusDto?> GetSessionPreferencesStatusAsync(string userId)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                (s.InitiatorId == userId || s.PartnerId == userId) &&
                (s.Status == SessionStatus.Active || s.Status == SessionStatus.WaitingForPartner));

        if (session == null)
        {
            return null;
        }

        return new SessionPreferencesStatusDto
        {
            SessionId = session.Id,
            SetupStatus = session.SetupStatus,
            InitiatorCompleted = session.InitiatorPrefsCompletedAt.HasValue,
            PartnerCompleted = session.PartnerPrefsCompletedAt.HasValue,
            InitiatorCompletedAt = session.InitiatorPrefsCompletedAt,
            PartnerCompletedAt = session.PartnerPrefsCompletedAt
        };
    }

    public async Task<IEnumerable<UserPreferenceDto>> GetUserPreferencesAsync(string userId)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                (s.InitiatorId == userId || s.PartnerId == userId) &&
                (s.Status == SessionStatus.Active || s.Status == SessionStatus.WaitingForPartner));

        if (session == null)
        {
            return Enumerable.Empty<UserPreferenceDto>();
        }

        var preferences = await _context.UserPreferences
            .Include(p => p.Category)
            .Where(p => p.UserId == userId && p.SessionId == session.Id)
            .Select(p => new UserPreferenceDto
            {
                Id = p.Id,
                CategoryId = p.CategoryId,
                CategoryCode = p.Category.Code,
                CategoryName = p.Category.DisplayName,
                CategoryType = p.Category.CategoryType,
                Level = p.Level
            })
            .ToListAsync();

        return preferences;
    }
}
