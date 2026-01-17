using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;
using NameMatch.Application.DTOs.Preferences;
using NameMatch.Application.Interfaces;

namespace NameMatch.Api.Controllers;

/// <summary>
/// Preference management endpoints for setting and retrieving user name preferences.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class PreferencesController : ControllerBase
{
    private readonly IPreferenceService _preferenceService;

    public PreferencesController(IPreferenceService preferenceService)
    {
        _preferenceService = preferenceService;
    }

    /// <summary>
    /// Get all available name categories.
    /// </summary>
    /// <param name="type">Optional filter by category type (ORIGIN, STYLE, SOUND).</param>
    /// <returns>List of categories.</returns>
    /// <response code="200">Categories retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("categories")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<CategoryDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<CategoryDto>>>> GetCategories([FromQuery] string? type = null)
    {
        var categories = await _preferenceService.GetCategoriesAsync(type);
        return Ok(ApiResponse<IEnumerable<CategoryDto>>.Ok(categories));
    }

    /// <summary>
    /// Get the preference questionnaire with all questions and options.
    /// </summary>
    /// <returns>List of questions with options.</returns>
    /// <response code="200">Questions retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("questions")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PreferenceQuestionDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<PreferenceQuestionDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<PreferenceQuestionDto>>>> GetQuestions()
    {
        var questions = await _preferenceService.GetQuestionsAsync();
        return Ok(ApiResponse<IEnumerable<PreferenceQuestionDto>>.Ok(questions));
    }

    /// <summary>
    /// Submit user preferences for the current session.
    /// </summary>
    /// <param name="request">Preference answers.</param>
    /// <returns>Updated session preferences status.</returns>
    /// <response code="200">Preferences saved successfully.</response>
    /// <response code="400">No active session or invalid request.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SessionPreferencesStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionPreferencesStatusDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionPreferencesStatusDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SessionPreferencesStatusDto>>> SubmitPreferences([FromBody] SubmitPreferencesRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionPreferencesStatusDto>.Fail("User not found"));

        try
        {
            var status = await _preferenceService.SubmitPreferencesAsync(userId, request);
            return Ok(ApiResponse<SessionPreferencesStatusDto>.Ok(status, "Preferences saved successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SessionPreferencesStatusDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get the preferences completion status for the current session.
    /// </summary>
    /// <returns>Status showing if both partners have completed preferences.</returns>
    /// <response code="200">Status retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">No active session found.</response>
    [HttpGet("status")]
    [ProducesResponseType(typeof(ApiResponse<SessionPreferencesStatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionPreferencesStatusDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SessionPreferencesStatusDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SessionPreferencesStatusDto>>> GetPreferencesStatus()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionPreferencesStatusDto>.Fail("User not found"));

        var status = await _preferenceService.GetSessionPreferencesStatusAsync(userId);
        if (status == null)
            return NotFound(ApiResponse<SessionPreferencesStatusDto>.Fail("No active session found"));

        return Ok(ApiResponse<SessionPreferencesStatusDto>.Ok(status));
    }

    /// <summary>
    /// Get the current user's preferences for their active session.
    /// </summary>
    /// <returns>List of user preferences.</returns>
    /// <response code="200">Preferences retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("mine")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserPreferenceDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<UserPreferenceDto>>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IEnumerable<UserPreferenceDto>>>> GetMyPreferences()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<IEnumerable<UserPreferenceDto>>.Fail("User not found"));

        var preferences = await _preferenceService.GetUserPreferencesAsync(userId);
        return Ok(ApiResponse<IEnumerable<UserPreferenceDto>>.Ok(preferences));
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
    }
}
