using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Preferences;

public class SessionPreferencesStatusDto
{
    public Guid SessionId { get; set; }
    public SessionSetupStatus SetupStatus { get; set; }
    public bool InitiatorCompleted { get; set; }
    public bool PartnerCompleted { get; set; }
    public DateTime? InitiatorCompletedAt { get; set; }
    public DateTime? PartnerCompletedAt { get; set; }
    public bool BothCompleted => InitiatorCompleted && PartnerCompleted;
    public bool CanStartVoting => BothCompleted;
}
