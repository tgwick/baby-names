using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;
using NameMatch.Application.DTOs.Filters;
using NameMatch.Application.Interfaces;

namespace NameMatch.Api.Controllers;

/// <summary>
/// Filter management endpoints for setting hard filters that exclude names from the pool.
/// Filters run before soft preferences and dramatically reduce the available name pool.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class FiltersController : ControllerBase
{
    private readonly IFilterService _filterService;

    public FiltersController(IFilterService filterService)
    {
        _filterService = filterService;
    }

    /// <summary>
    /// Get the filter questions with options.
    /// </summary>
    /// <returns>List of filter questions.</returns>
    /// <response code="200">Questions retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("questions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FilterQuestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<FilterQuestionDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<FilterQuestionDto>>>> GetQuestions()
    {
        var questions = await _filterService.GetFilterQuestionsAsync();
        return Ok(ApiResponse<IEnumerable<FilterQuestionDto>>.Ok(questions));
    }

    /// <summary>
    /// Submit filter answers for the specified session.
    /// </summary>
    /// <param name="request">Filter answers including session ID.</param>
    /// <returns>Updated filter status.</returns>
    /// <response code="200">Filters saved successfully.</response>
    /// <response code="400">Session not found or invalid request.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SessionFiltersStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionFiltersStatusDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionFiltersStatusDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SessionFiltersStatusDto>>> SubmitFilters([FromBody] SubmitFiltersRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionFiltersStatusDto>.Fail("User not found"));

        try
        {
            var status = await _filterService.SubmitFiltersAsync(userId, request.SessionId, request);
            return Ok(ApiResponse<SessionFiltersStatusDto>.Ok(status, "Filters saved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SessionFiltersStatusDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get the filter completion status for the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>Status showing if both partners have completed filters.</returns>
    /// <response code="200">Status retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">Session not found.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<SessionFiltersStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionFiltersStatusDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SessionFiltersStatusDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SessionFiltersStatusDto>>> GetFiltersStatus([FromQuery] Guid sessionId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionFiltersStatusDto>.Fail("User not found"));

        var status = await _filterService.GetFiltersStatusAsync(userId, sessionId);
        if (status == null)
            return NotFound(ApiResponse<SessionFiltersStatusDto>.Fail("Session not found"));

        return Ok(ApiResponse<SessionFiltersStatusDto>.Ok(status));
    }

    /// <summary>
    /// Get the current user's filters for the specified session.
    /// </summary>
    /// <param name="sessionId">The session ID.</param>
    /// <returns>User's filter settings.</returns>
    /// <response code="200">Filters retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">No filters found.</response>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<UserFiltersDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserFiltersDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserFiltersDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserFiltersDto>>> GetMyFilters([FromQuery] Guid sessionId)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<UserFiltersDto>.Fail("User not found"));

        var filters = await _filterService.GetUserFiltersAsync(userId, sessionId);
        if (filters == null)
            return NotFound(ApiResponse<UserFiltersDto>.Fail("No filters found for this session"));

        return Ok(ApiResponse<UserFiltersDto>.Ok(filters));
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
    }
}
