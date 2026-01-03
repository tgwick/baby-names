namespace NameMatch.Application.DTOs.Name;

public class NameDto
{
    public int Id { get; set; }
    public required string NameText { get; set; }
    public int Gender { get; set; }
    public int PopularityScore { get; set; }
    public string? Origin { get; set; }
}
