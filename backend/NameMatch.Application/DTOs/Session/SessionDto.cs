using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Session;

public class SessionDto
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
    public bool IsInitiator { get; set; }
    public string? PartnerDisplayName { get; set; }
    public string? InitiatorDisplayName { get; set; }

    // Preference tracking (legacy)
    public SessionSetupStatus SetupStatus { get; set; }
    public bool InitiatorPrefsCompleted { get; set; }
    public bool PartnerPrefsCompleted { get; set; }

    // Filter tracking (new system)
    public bool InitiatorFiltersCompleted { get; set; }
    public bool PartnerFiltersCompleted { get; set; }

    // Can start voting when both partners have completed filters (or legacy prefs for backwards compatibility)
    public bool CanStartVoting => (InitiatorFiltersCompleted && PartnerFiltersCompleted) ||
                                   (InitiatorPrefsCompleted && PartnerPrefsCompleted);
}
