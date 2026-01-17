using NameMatch.Domain.Enums;

namespace NameMatch.Domain.Entities;

public class UserPreference
{
    public int Id { get; set; }
    public required string UserId { get; set; }
    public Guid SessionId { get; set; }
    public int CategoryId { get; set; }
    public PreferenceLevel Level { get; set; }
    public DateTime CreatedAt { get; set; }

    public Session Session { get; set; } = null!;
    public NameCategory Category { get; set; } = null!;
}
