using NameMatch.Application.DTOs.Session;

namespace NameMatch.Application.Interfaces;

public interface ISessionService
{
    Task<SessionDto> CreateSessionAsync(string userId, CreateSessionRequest request);
    Task<SessionDto> JoinByCodeAsync(string userId, string joinCode);
    Task<SessionDto> JoinByLinkAsync(string userId, string partnerLink);
    Task<SessionDto?> GetCurrentSessionAsync(string userId);
    Task<SessionDto?> GetSessionByIdAsync(Guid sessionId, string userId);
    Task<SessionListResponseDto> GetUserSessionsAsync(string userId, bool includeArchived = false);
    Task<SessionDto> ArchiveSessionAsync(Guid sessionId, string userId);
    Task<SessionDto> UnarchiveSessionAsync(Guid sessionId, string userId);
}
