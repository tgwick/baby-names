using System.ComponentModel.DataAnnotations;

namespace NameMatch.Application.DTOs.Filters;

public class SubmitFiltersRequest
{
    [Required]
    public Guid SessionId { get; set; }

    public required List<FilterAnswerDto> Answers { get; set; }
}

public class FilterAnswerDto
{
    public required string QuestionId { get; set; }
    public required string SelectedOptionId { get; set; }
}
