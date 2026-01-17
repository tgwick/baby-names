namespace NameMatch.Application.DTOs.Preferences;

public class PreferenceQuestionDto
{
    public required string QuestionId { get; set; }
    public required string QuestionText { get; set; }
    public required string CategoryType { get; set; }
    public bool AllowMultiple { get; set; }
    public required List<PreferenceOptionDto> Options { get; set; }
}

public class PreferenceOptionDto
{
    public required string OptionId { get; set; }
    public required string Label { get; set; }
    public string? Description { get; set; }
    public required List<string> CategoryCodes { get; set; }
    public int PreferenceLevel { get; set; }
}
