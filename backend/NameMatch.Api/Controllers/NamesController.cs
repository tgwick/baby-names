using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;
using NameMatch.Application.DTOs.Name;
using NameMatch.Application.Interfaces;

namespace NameMatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NamesController : ControllerBase
{
    private readonly INameService _nameService;

    public NamesController(INameService nameService)
    {
        _nameService = nameService;
    }

    /// <summary>
    /// Gets the next unvoted name for the current user's active session.
    /// Returns names filtered by the session's target gender.
    /// </summary>
    [HttpGet("next")]
    public async Task<ActionResult<ApiResponse<NameDto>>> GetNextName()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<NameDto>.Fail("User not found"));

        var name = await _nameService.GetNextUnvotedNameAsync(userId);

        if (name == null)
        {
            return Ok(ApiResponse<NameDto>.Ok(null!, "No more names available"));
        }

        return Ok(ApiResponse<NameDto>.Ok(name));
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
    }
}
