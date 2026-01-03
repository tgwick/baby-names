using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;
using NameMatch.Application.DTOs.Name;
using NameMatch.Application.Interfaces;

namespace NameMatch.Api.Controllers;

/// <summary>
/// Endpoints for retrieving baby names to vote on.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class NamesController : ControllerBase
{
    private readonly INameService _nameService;

    public NamesController(INameService nameService)
    {
        _nameService = nameService;
    }

    /// <summary>
    /// Gets the next unvoted name for the current user's active session.
    /// </summary>
    /// <remarks>
    /// Returns names filtered by the session's target gender.
    /// Returns null data with success message when no more names are available.
    /// </remarks>
    /// <returns>Next unvoted name, or null if none remaining.</returns>
    /// <response code="200">Name retrieved (or null if no more available).</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("next")]
    [ProducesResponseType(typeof(ApiResponse<NameDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<NameDto>), StatusCodes.Status401Unauthorized)]
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
