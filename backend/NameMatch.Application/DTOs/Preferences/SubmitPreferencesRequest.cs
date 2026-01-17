using System.ComponentModel.DataAnnotations;

namespace NameMatch.Application.DTOs.Preferences;

public class SubmitPreferencesRequest
{
    [Required]
    public required List<PreferenceAnswerDto> Answers { get; set; }
}

public class PreferenceAnswerDto
{
    [Required]
    public required string QuestionId { get; set; }

    [Required]
    public required List<string> SelectedOptionIds { get; set; }
}
