using NameMatch.Application.DTOs.Name;

namespace NameMatch.Application.Interfaces;

public interface INameService
{
    /// <summary>
    /// Gets the next unvoted name for a user in the specified session.
    /// </summary>
    Task<NameDto?> GetNextUnvotedNameAsync(string userId, Guid sessionId);

    /// <summary>
    /// Gets the count of names available for the session's target gender.
    /// </summary>
    Task<int> GetNameCountForSessionAsync(Guid sessionId);
}
