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
    /// Submit filter answers for the current session.
    /// </summary>
    /// <param name="request">Filter answers.</param>
    /// <returns>Updated filter status.</returns>
    /// <response code="200">Filters saved successfully.</response>
    /// <response code="400">No active session or invalid request.</response>
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
            var status = await _filterService.SubmitFiltersAsync(userId, request);
            return Ok(ApiResponse<SessionFiltersStatusDto>.Ok(status, "Filters saved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SessionFiltersStatusDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get the filter completion status for the current session.
    /// </summary>
    /// <returns>Status showing if both partners have completed filters.</returns>
    /// <response code="200">Status retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">No active session found.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<SessionFiltersStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionFiltersStatusDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SessionFiltersStatusDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SessionFiltersStatusDto>>> GetFiltersStatus()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionFiltersStatusDto>.Fail("User not found"));

        var status = await _filterService.GetFiltersStatusAsync(userId);
        if (status == null)
            return NotFound(ApiResponse<SessionFiltersStatusDto>.Fail("No active session found"));

        return Ok(ApiResponse<SessionFiltersStatusDto>.Ok(status));
    }

    /// <summary>
    /// Get the current user's filters for their active session.
    /// </summary>
    /// <returns>User's filter settings.</returns>
    /// <response code="200">Filters retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">No filters found.</response>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<UserFiltersDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserFiltersDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserFiltersDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserFiltersDto>>> GetMyFilters()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<UserFiltersDto>.Fail("User not found"));

        var filters = await _filterService.GetUserFiltersAsync(userId);
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
