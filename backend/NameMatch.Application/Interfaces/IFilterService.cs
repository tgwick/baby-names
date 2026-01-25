using NameMatch.Application.DTOs.Filters;

namespace NameMatch.Application.Interfaces;

public interface IFilterService
{
    /// <summary>
    /// Gets all filter questions with their options.
    /// </summary>
    Task<IEnumerable<FilterQuestionDto>> GetFilterQuestionsAsync();

    /// <summary>
    /// Submits filter answers for the specified session.
    /// </summary>
    Task<SessionFiltersStatusDto> SubmitFiltersAsync(string userId, Guid sessionId, SubmitFiltersRequest request);

    /// <summary>
    /// Gets the filter completion status for a session.
    /// </summary>
    Task<SessionFiltersStatusDto?> GetFiltersStatusAsync(string userId, Guid sessionId);

    /// <summary>
    /// Gets the user's filters for the specified session.
    /// </summary>
    Task<UserFiltersDto?> GetUserFiltersAsync(string userId, Guid sessionId);

    /// <summary>
    /// Gets the combined filters from both partners in a session.
    /// Uses intersection logic to create the most restrictive filter set.
    /// </summary>
    Task<CombinedFiltersDto?> GetCombinedSessionFiltersAsync(Guid sessionId);
}
