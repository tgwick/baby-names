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

    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}
