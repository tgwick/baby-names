using System.ComponentModel.DataAnnotations;
using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Vote;

public class SubmitVoteRequest
{
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "NameId must be a positive integer")]
    public int NameId { get; set; }

    [Required]
    [EnumDataType(typeof(VoteType), ErrorMessage = "Invalid vote type")]
    public VoteType VoteType { get; set; }
}
