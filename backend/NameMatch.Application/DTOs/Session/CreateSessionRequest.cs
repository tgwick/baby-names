using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Session;

public class CreateSessionRequest
{
    public Gender TargetGender { get; set; }
}
