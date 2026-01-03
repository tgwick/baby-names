using NameMatch.Application.DTOs.Name;

namespace NameMatch.Application.Interfaces;

public interface INameService
{
    /// <summary>
    /// Gets the next unvoted name for a user in their current session.
    /// </summary>
    Task<NameDto?> GetNextUnvotedNameAsync(string userId);

    /// <summary>
    /// Gets the count of names available for the session's target gender.
    /// </summary>
    Task<int> GetNameCountForSessionAsync(Guid sessionId);
}
