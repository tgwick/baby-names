using System.ComponentModel.DataAnnotations;
using NameMatch.Domain.Enums;

namespace NameMatch.Application.DTOs.Session;

public class CreateSessionRequest
{
    [Required]
    [EnumDataType(typeof(Gender), ErrorMessage = "Invalid gender value")]
    public Gender TargetGender { get; set; }
}
