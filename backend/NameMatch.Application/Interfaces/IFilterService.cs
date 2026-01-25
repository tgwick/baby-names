using NameMatch.Application.DTOs.Filters;

namespace NameMatch.Application.Interfaces;

public interface IFilterService
{
    /// <summary>
    /// Gets all filter questions with their options.
    /// </summary>
    Task<IEnumerable<FilterQuestionDto>> GetFilterQuestionsAsync();

    /// <summary>
    /// Submits filter answers for the current user's session.
    /// </summary>
    Task<SessionFiltersStatusDto> SubmitFiltersAsync(string userId, SubmitFiltersRequest request);

    /// <summary>
    /// Gets the filter completion status for a session.
    /// </summary>
    Task<SessionFiltersStatusDto?> GetFiltersStatusAsync(string userId);

    /// <summary>
    /// Gets the current user's filters for their active session.
    /// </summary>
    Task<UserFiltersDto?> GetUserFiltersAsync(string userId);

    /// <summary>
    /// Gets the combined filters from both partners in a session.
    /// Uses intersection logic to create the most restrictive filter set.
    /// </summary>
    Task<CombinedFiltersDto?> GetCombinedSessionFiltersAsync(Guid sessionId);
}
