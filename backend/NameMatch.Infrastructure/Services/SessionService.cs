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
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(request);

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
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(joinCode))
        {
            throw new ArgumentException("Join code is required.", nameof(joinCode));
        }

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
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(partnerLink))
        {
            throw new ArgumentException("Partner link is required.", nameof(partnerLink));
        }

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
            PartnerDisplayName = partner?.DisplayName ?? partner?.Email,
            SetupStatus = session.SetupStatus,
            InitiatorPrefsCompleted = session.InitiatorPrefsCompletedAt.HasValue,
            PartnerPrefsCompleted = session.PartnerPrefsCompletedAt.HasValue,
            InitiatorFiltersCompleted = session.InitiatorFiltersCompletedAt.HasValue,
            PartnerFiltersCompleted = session.PartnerFiltersCompletedAt.HasValue,
            IsArchived = session.IsArchived,
            ArchivedAt = session.ArchivedAt
        };
    }

    public async Task<SessionListResponseDto> GetUserSessionsAsync(string userId, bool includeArchived = false)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var query = _context.Sessions
            .Where(s => s.InitiatorId == userId || s.PartnerId == userId);

        if (!includeArchived)
        {
            query = query.Where(s => !s.IsArchived);
        }

        var sessions = await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        var archivedCount = await _context.Sessions
            .Where(s => (s.InitiatorId == userId || s.PartnerId == userId) && s.IsArchived)
            .CountAsync();

        var sessionItems = new List<SessionListItemDto>();

        foreach (var session in sessions)
        {
            var partnerId = session.InitiatorId == userId ? session.PartnerId : session.InitiatorId;
            string? partnerDisplayName = null;

            if (partnerId != null)
            {
                var partner = await _userManager.FindByIdAsync(partnerId);
                partnerDisplayName = partner?.DisplayName ?? partner?.Email;
            }

            // Get match count for this session
            var matchCount = await GetMatchCountForSessionAsync(session);

            // Get vote count for this user in this session
            var voteCount = await _context.Votes
                .Where(v => v.UserId == userId && v.SessionId == session.Id)
                .CountAsync();

            sessionItems.Add(new SessionListItemDto
            {
                Id = session.Id,
                PartnerDisplayName = partnerDisplayName,
                CreatedAt = session.CreatedAt,
                Status = session.Status,
                IsArchived = session.IsArchived,
                TargetGender = session.TargetGender,
                MatchCount = matchCount,
                VoteCount = voteCount,
                IsInitiator = session.InitiatorId == userId
            });
        }

        return new SessionListResponseDto
        {
            Sessions = sessionItems,
            TotalCount = sessionItems.Count,
            ArchivedCount = archivedCount
        };
    }

    public async Task<SessionDto> ArchiveSessionAsync(Guid sessionId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId &&
                (s.InitiatorId == userId || s.PartnerId == userId));

        if (session == null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        if (session.IsArchived)
        {
            throw new InvalidOperationException("Session is already archived.");
        }

        session.IsArchived = true;
        session.ArchivedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await MapToDto(session, userId);
    }

    public async Task<SessionDto> UnarchiveSessionAsync(Guid sessionId, string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID is required.", nameof(userId));
        }

        var session = await _context.Sessions
            .FirstOrDefaultAsync(s => s.Id == sessionId &&
                (s.InitiatorId == userId || s.PartnerId == userId));

        if (session == null)
        {
            throw new InvalidOperationException("Session not found.");
        }

        if (!session.IsArchived)
        {
            throw new InvalidOperationException("Session is not archived.");
        }

        session.IsArchived = false;
        session.ArchivedAt = null;

        await _context.SaveChangesAsync();

        return await MapToDto(session, userId);
    }

    private async Task<int> GetMatchCountForSessionAsync(Session session)
    {
        if (session.PartnerId == null)
        {
            return 0;
        }

        var matchCount = await (
            from initiatorVote in _context.Votes
            join partnerVote in _context.Votes
                on new { initiatorVote.NameId, initiatorVote.SessionId }
                equals new { partnerVote.NameId, partnerVote.SessionId }
            where initiatorVote.UserId == session.InitiatorId
                && partnerVote.UserId == session.PartnerId
                && initiatorVote.SessionId == session.Id
                && initiatorVote.VoteType == VoteType.Like
                && partnerVote.VoteType == VoteType.Like
            select initiatorVote.Id
        ).CountAsync();

        return matchCount;
    }
}
