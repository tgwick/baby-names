namespace NameMatch.Application.DTOs.Filters;

public class SessionFiltersStatusDto
{
    public Guid SessionId { get; set; }
    public bool InitiatorCompleted { get; set; }
    public bool PartnerCompleted { get; set; }
    public DateTime? InitiatorCompletedAt { get; set; }
    public DateTime? PartnerCompletedAt { get; set; }
    public bool BothCompleted => InitiatorCompleted && PartnerCompleted;
}
