using NameMatch.Application.DTOs.Session;

namespace NameMatch.Application.Interfaces;

public interface ISessionService
{
    Task<SessionDto> CreateSessionAsync(string userId, CreateSessionRequest request);
    Task<SessionDto> JoinByCodeAsync(string userId, string joinCode);
    Task<SessionDto> JoinByLinkAsync(string userId, string partnerLink);
    Task<SessionDto?> GetCurrentSessionAsync(string userId);
    Task<SessionDto?> GetSessionByIdAsync(Guid sessionId, string userId);
}
