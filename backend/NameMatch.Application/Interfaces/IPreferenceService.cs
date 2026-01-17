using NameMatch.Application.DTOs.Preferences;

namespace NameMatch.Application.Interfaces;

public interface IPreferenceService
{
    /// <summary>
    /// Gets all available categories, optionally filtered by type.
    /// </summary>
    Task<IEnumerable<CategoryDto>> GetCategoriesAsync(string? categoryType = null);

    /// <summary>
    /// Gets the preference questionnaire with predefined questions.
    /// </summary>
    Task<IEnumerable<PreferenceQuestionDto>> GetQuestionsAsync();

    /// <summary>
    /// Submits user preferences for the current session.
    /// </summary>
    Task<SessionPreferencesStatusDto> SubmitPreferencesAsync(string userId, SubmitPreferencesRequest request);

    /// <summary>
    /// Gets the preference completion status for a session.
    /// </summary>
    Task<SessionPreferencesStatusDto?> GetSessionPreferencesStatusAsync(string userId);

    /// <summary>
    /// Gets the current user's preferences for their active session.
    /// </summary>
    Task<IEnumerable<UserPreferenceDto>> GetUserPreferencesAsync(string userId);
}
