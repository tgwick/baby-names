using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using NameMatch.Application.DTOs.Session;
using NameMatch.Application.Interfaces;
using NameMatch.Domain.Entities;
using NameMatch.Domain.Enums;
using NameMatch.Infrastructure.Data;
using NameMatch.Infrastructure.Identity;

namespace NameMatch.Infrastructure.Services;

public class SessionService : ISessionService
{
    private readonly ApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public SessionService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<SessionDto> CreateSessionAsync(string userId, CreateSessionRequest request)
    {
        // Check if user already has an active session
        var existingSession = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                (s.InitiatorId == userId || s.PartnerId == userId) &&
                s.Status != SessionStatus.Completed);

        if (existingSession != null)
        {
            throw new InvalidOperationException("You already have an active session. Complete or leave it before creating a new one.");
        }

        var session = new Session
        {
            Id = Guid.NewGuid(),
            InitiatorId = userId,
            TargetGender = request.TargetGender,
            JoinCode = await GenerateUniqueJoinCodeAsync(),
            PartnerLink = Guid.NewGuid().ToString("N")[..12],
            Status = SessionStatus.WaitingForPartner,
            CreatedAt = DateTime.UtcNow
        };

        _context.Sessions.Add(session);
        await _context.SaveChangesAsync();

        return await MapToDto(session, userId);
    }

    public async Task<SessionDto> JoinByCodeAsync(string userId, string joinCode)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.JoinCode == joinCode.ToUpper());

        if (session == null)
        {
            throw new InvalidOperationException("Session not found. Please check the code and try again.");
        }

        return await JoinSessionAsync(session, userId);
    }

    public async Task<SessionDto> JoinByLinkAsync(string userId, string partnerLink)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.PartnerLink == partnerLink);

        if (session == null)
        {
            throw new InvalidOperationException("Session not found. The link may be invalid or expired.");
        }

        return await JoinSessionAsync(session, userId);
    }

    public async Task<SessionDto?> GetCurrentSessionAsync(string userId)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                (s.InitiatorId == userId || s.PartnerId == userId) &&
                s.Status != SessionStatus.Completed);

        if (session == null)
        {
            return null;
        }

        return await MapToDto(session, userId);
    }

    public async Task<SessionDto?> GetSessionByIdAsync(Guid sessionId, string userId)
    {
        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId &&
                (s.InitiatorId == userId || s.PartnerId == userId));

        if (session == null)
        {
            return null;
        }

        return await MapToDto(session, userId);
    }

    private async Task<SessionDto> JoinSessionAsync(Session session, string userId)
    {
        if (session.InitiatorId == userId)
        {
            throw new InvalidOperationException("You cannot join your own session.");
        }

        if (session.PartnerId != null)
        {
            if (session.PartnerId == userId)
            {
                // Already in this session, just return it
                return await MapToDto(session, userId);
            }
            throw new InvalidOperationException("This session already has a partner.");
        }

        // Check if user already has an active session
        var existingSession = await _context.Sessions
            .FirstOrDefaultAsync(s =>
                (s.InitiatorId == userId || s.PartnerId == userId) &&
                s.Status != SessionStatus.Completed);

        if (existingSession != null)
        {
            throw new InvalidOperationException("You already have an active session. Complete or leave it before joining a new one.");
        }

        session.PartnerId = userId;
        session.Status = SessionStatus.Active;
        session.LinkedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await MapToDto(session, userId);
    }

    private async Task<string> GenerateUniqueJoinCodeAsync()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Removed confusing chars (0, O, 1, I)
        var random = new Random();
        string code;

        do
        {
            code = new string(Enumerable.Range(0, 6)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        }
        while (await _context.Sessions.AnyAsync(s => s.JoinCode == code));

        return code;
    }

    private async Task<SessionDto> MapToDto(Session session, string currentUserId)
    {
        var initiator = await _userManager.FindByIdAsync(session.InitiatorId);
        ApplicationUser? partner = null;
        if (session.PartnerId != null)
        {
            partner = await _userManager.FindByIdAsync(session.PartnerId);
        }

        return new SessionDto
        {
            Id = session.Id,
            InitiatorId = session.InitiatorId,
            PartnerId = session.PartnerId,
            TargetGender = session.TargetGender,
            JoinCode = session.JoinCode,
            PartnerLink = session.PartnerLink,
            Status = session.Status,
            CreatedAt = session.CreatedAt,
            LinkedAt = session.LinkedAt,
            IsInitiator = session.InitiatorId == currentUserId,
            InitiatorDisplayName = initiator?.DisplayName ?? initiator?.Email,
            PartnerDisplayName = partner?.DisplayName ?? partner?.Email
        };
    }
}
