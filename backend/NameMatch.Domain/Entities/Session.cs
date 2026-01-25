using NameMatch.Domain.Enums;

namespace NameMatch.Domain.Entities;

public class Session
{
    public Guid Id { get; set; }
    public required string InitiatorId { get; set; }
    public string? PartnerId { get; set; }
    public Gender TargetGender { get; set; }
    public required string JoinCode { get; set; }
    public required string PartnerLink { get; set; }
    public SessionStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LinkedAt { get; set; }

    // Preference tracking
    public SessionSetupStatus SetupStatus { get; set; }
    public DateTime? InitiatorPrefsCompletedAt { get; set; }
    public DateTime? PartnerPrefsCompletedAt { get; set; }

    // Filter tracking (hard filters run before soft preferences)
    public DateTime? InitiatorFiltersCompletedAt { get; set; }
    public DateTime? PartnerFiltersCompletedAt { get; set; }

    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
    public ICollection<UserPreference> UserPreferences { get; set; } = new List<UserPreference>();
    public ICollection<SessionFilter> Filters { get; set; } = new List<SessionFilter>();
}
