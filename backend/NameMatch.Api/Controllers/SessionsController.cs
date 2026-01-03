using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;
using NameMatch.Application.DTOs.Session;
using NameMatch.Application.Interfaces;

namespace NameMatch.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SessionsController : ControllerBase
{
    private readonly ISessionService _sessionService;

    public SessionsController(ISessionService sessionService)
    {
        _sessionService = sessionService;
    }

    [HttpPost]
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

    [HttpPost("join")]
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

    [HttpGet("join/{partnerLink}")]
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

    [HttpGet("current")]
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

    [HttpGet("{id:guid}")]
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
