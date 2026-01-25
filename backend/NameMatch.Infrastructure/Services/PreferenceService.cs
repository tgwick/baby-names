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
        // Simplified questionnaire - just 2 key questions
        var questions = new List<PreferenceQuestionDto>
        {
            new()
            {
                QuestionId = "style",
                QuestionText = "What style of name do you prefer?",
                CategoryType = "STYLE",
                AllowMultiple = false,
                Options = new List<PreferenceOptionDto>
                {
                    new() { OptionId = "classic", Label = "Classic & Timeless", Description = "Traditional names that never go out of style (e.g., James, Elizabeth)", CategoryCodes = new List<string> { "CLASSIC" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "trendy", Label = "Trendy & Modern", Description = "Currently popular, contemporary names (e.g., Liam, Olivia)", CategoryCodes = new List<string> { "TRENDY", "MODERN" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "unique", Label = "Unique & Rare", Description = "Names that stand out from the crowd (e.g., Zephyr, Aurelia)", CategoryCodes = new List<string> { "UNIQUE" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "no_pref", Label = "No preference", Description = "Open to all styles", CategoryCodes = new List<string>(), PreferenceLevel = (int)PreferenceLevel.Neutral }
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
                    new() { OptionId = "short", Label = "Short & Punchy", Description = "1-2 syllables (e.g., Max, Kate, Leo)", CategoryCodes = new List<string> { "SHORT" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "long", Label = "Flowing & Elegant", Description = "3+ syllables (e.g., Alexander, Isabella)", CategoryCodes = new List<string> { "LONG" }, PreferenceLevel = (int)PreferenceLevel.Love },
                    new() { OptionId = "no_pref", Label = "No preference", Description = "Open to any length", CategoryCodes = new List<string>(), PreferenceLevel = (int)PreferenceLevel.Neutral }
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
