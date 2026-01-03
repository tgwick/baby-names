using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;
using NameMatch.Application.DTOs.Vote;
using NameMatch.Application.Interfaces;

namespace NameMatch.Api.Controllers;

/// <summary>
/// Voting endpoints for submitting likes/dislikes on baby names and retrieving matches.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class VotesController : ControllerBase
{
    private readonly IVoteService _voteService;

    public VotesController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    /// <summary>
    /// Submit a vote (Like/Dislike) for a name.
    /// </summary>
    /// <param name="request">Vote details including name ID and vote type (Like=0, Dislike=1).</param>
    /// <returns>Vote result including whether this created a match.</returns>
    /// <response code="200">Vote submitted successfully.</response>
    /// <response code="400">Invalid request or no active session.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<VoteResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VoteResultDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<VoteResultDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<VoteResultDto>>> SubmitVote([FromBody] SubmitVoteRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<VoteResultDto>.Fail("User not found"));

        try
        {
            var result = await _voteService.SubmitVoteAsync(userId, request.NameId, request.VoteType);
            return Ok(ApiResponse<VoteResultDto>.Ok(result));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<VoteResultDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get all matches (mutual likes) for the current session.
    /// </summary>
    /// <returns>List of names both users have liked.</returns>
    /// <response code="200">Matches retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("matches")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MatchDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<MatchDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<MatchDto>>>> GetMatches()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<IEnumerable<MatchDto>>.Fail("User not found"));

        var matches = await _voteService.GetMatchesAsync(userId);
        return Ok(ApiResponse<IEnumerable<MatchDto>>.Ok(matches));
    }

    /// <summary>
    /// Get the count of matches for the current session.
    /// </summary>
    /// <returns>Number of mutual likes.</returns>
    /// <response code="200">Match count retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("matches/count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<int>>> GetMatchCount()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<int>.Fail("User not found"));

        var count = await _voteService.GetMatchCountAsync(userId);
        return Ok(ApiResponse<int>.Ok(count));
    }

    /// <summary>
    /// Get all votes by the current user in their active session.
    /// </summary>
    /// <returns>List of all votes submitted by the user.</returns>
    /// <response code="200">Votes retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<VoteDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<VoteDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<VoteDto>>>> GetVotes()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<IEnumerable<VoteDto>>.Fail("User not found"));

        var votes = await _voteService.GetUserVotesAsync(userId);
        return Ok(ApiResponse<IEnumerable<VoteDto>>.Ok(votes));
    }

    /// <summary>
    /// Get voting statistics for the current session.
    /// </summary>
    /// <returns>Statistics including like/dislike counts, match count, and remaining names.</returns>
    /// <response code="200">Stats retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("stats")]
    [ProducesResponseType(typeof(ApiResponse<VoteStatsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<VoteStatsDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<VoteStatsDto>>> GetStats()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<VoteStatsDto>.Fail("User not found"));

        var stats = await _voteService.GetVoteStatsAsync(userId);
        return Ok(ApiResponse<VoteStatsDto>.Ok(stats));
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
    }
}
