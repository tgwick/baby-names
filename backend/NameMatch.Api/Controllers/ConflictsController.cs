using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;
using NameMatch.Application.DTOs.Vote;
using NameMatch.Application.Interfaces;

namespace NameMatch.Api.Controllers;

/// <summary>
/// Conflict resolution endpoints for handling disagreements on names.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class ConflictsController : ControllerBase
{
    private readonly IVoteService _voteService;

    public ConflictsController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    /// <summary>
    /// Get all conflicts for the current session.
    /// </summary>
    /// <remarks>
    /// A conflict occurs when one user likes a name and the other dislikes it.
    /// </remarks>
    /// <returns>List of conflicting names with details about who liked/disliked.</returns>
    /// <response code="200">Conflicts retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConflictDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<ConflictDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<ConflictDto>>>> GetConflicts()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<IEnumerable<ConflictDto>>.Fail("User not found"));

        var conflicts = await _voteService.GetConflictsAsync(userId);
        return Ok(ApiResponse<IEnumerable<ConflictDto>>.Ok(conflicts));
    }

    /// <summary>
    /// Clear a dislike vote on a name, removing it from conflicts.
    /// </summary>
    /// <remarks>
    /// The name will return to the voting pool for the user who cleared their dislike.
    /// Only the user who disliked the name can clear their vote.
    /// </remarks>
    /// <param name="nameId">The ID of the name to clear the dislike vote for.</param>
    /// <returns>True if the dislike was cleared successfully.</returns>
    /// <response code="200">Dislike cleared successfully.</response>
    /// <response code="400">Vote not found or not a dislike vote.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost("{nameId}/clear")]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<bool>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<bool>>> ClearDislike(int nameId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<bool>.Fail("User not found"));

        try
        {
            var result = await _voteService.ClearDislikeAsync(userId, nameId);
            return Ok(ApiResponse<bool>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<bool>.Fail(ex.Message));
        }
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
    }
}
