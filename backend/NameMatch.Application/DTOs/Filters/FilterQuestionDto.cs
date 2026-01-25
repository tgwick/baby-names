namespace NameMatch.Application.DTOs.Filters;

public class FilterQuestionDto
{
    public required string QuestionId { get; set; }
    public required string QuestionText { get; set; }
    public required string FilterType { get; set; } // POPULARITY, SYLLABLES, ENDING_SOUND
    public required List<FilterOptionDto> Options { get; set; }
}

public class FilterOptionDto
{
    public required string OptionId { get; set; }
    public required string Label { get; set; }
    public string? Description { get; set; }
    public List<string>? ExampleNames { get; set; }

    // Values for the filter - meaning depends on FilterType
    public int? MinValue { get; set; }
    public int? MaxValue { get; set; }
    public List<string>? AllowedValues { get; set; } // For ending sounds
}
