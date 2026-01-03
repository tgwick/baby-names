using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;
using NameMatch.Application.DTOs.Session;
using NameMatch.Application.Interfaces;

namespace NameMatch.Api.Controllers;

/// <summary>
/// Session management endpoints for creating, joining, and managing name voting sessions.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    /// <summary>
    /// Create a new session for voting on baby names.
    /// </summary>
    /// <param name="request">Session creation details including target gender for names.</param>
    /// <returns>Created session with join code and partner link.</returns>
    /// <response code="200">Session created successfully.</response>
    /// <response code="400">User already has an active session.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> CreateSession([FromBody] CreateSessionRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionDto>.Fail("User not found"));

        try
        {
            var session = await _sessionService.CreateSessionAsync(userId, request);
            return Ok(ApiResponse<SessionDto>.Ok(session, "Session created successfully"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SessionDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Join an existing session using a 6-character join code.
    /// </summary>
    /// <param name="request">Join request containing the 6-character code.</param>
    /// <returns>Session details after joining.</returns>
    /// <response code="200">Successfully joined the session.</response>
    /// <response code="400">Invalid code, session not found, or already has an active session.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpPost("join")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> JoinByCode([FromBody] JoinSessionRequest request)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionDto>.Fail("User not found"));

        try
        {
            var session = await _sessionService.JoinByCodeAsync(userId, request.JoinCode);
            return Ok(ApiResponse<SessionDto>.Ok(session, "Successfully joined session"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SessionDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Join an existing session using a partner link.
    /// </summary>
    /// <param name="partnerLink">The unique partner link from the session creator.</param>
    /// <returns>Session details after joining.</returns>
    /// <response code="200">Successfully joined the session.</response>
    /// <response code="400">Invalid link, session not found, or already has an active session.</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("join/{partnerLink}")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> JoinByLink(string partnerLink)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionDto>.Fail("User not found"));

        try
        {
            var session = await _sessionService.JoinByLinkAsync(userId, partnerLink);
            return Ok(ApiResponse<SessionDto>.Ok(session, "Successfully joined session"));
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(ApiResponse<SessionDto>.Fail(ex.Message));
        }
    }

    /// <summary>
    /// Get the current user's active session.
    /// </summary>
    /// <returns>Current session details, or null if no active session.</returns>
    /// <response code="200">Session retrieved (or null if none active).</response>
    /// <response code="401">User not authenticated.</response>
    [HttpGet("current")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> GetCurrentSession()
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionDto>.Fail("User not found"));

        var session = await _sessionService.GetCurrentSessionAsync(userId);
        if (session == null)
            return Ok(ApiResponse<SessionDto>.Ok(null!, "No active session"));

        return Ok(ApiResponse<SessionDto>.Ok(session));
    }

    /// <summary>
    /// Get a specific session by ID.
    /// </summary>
    /// <param name="id">The session ID.</param>
    /// <returns>Session details.</returns>
    /// <response code="200">Session retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">Session not found or user not a member.</response>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SessionDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<SessionDto>>> GetSession(Guid id)
    {
        var userId = GetUserId();
        if (userId == null)
            return Unauthorized(ApiResponse<SessionDto>.Fail("User not found"));

        var session = await _sessionService.GetSessionByIdAsync(id, userId);
        if (session == null)
            return NotFound(ApiResponse<SessionDto>.Fail("Session not found"));

        return Ok(ApiResponse<SessionDto>.Ok(session));
    }

    private string? GetUserId()
    {
        return User.FindFirstValue(ClaimTypes.NameIdentifier)
               ?? User.FindFirstValue("sub");
    }
}
