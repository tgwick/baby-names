namespace NameMatch.Application.DTOs.Session;

public class SessionListResponseDto
{
    public List<SessionListItemDto> Sessions { get; set; } = new();
    public int TotalCount { get; set; }
    public int ArchivedCount { get; set; }
}
