using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using NameMatch.Application.DTOs;
using NameMatch.Application.DTOs.Auth;
using NameMatch.Application.Interfaces;
using NameMatch.Infrastructure.Identity;

namespace NameMatch.Api.Controllers;

/// <summary>
/// Authentication endpoints for user registration, login, and profile management.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IJwtService _jwtService;

    public AuthController(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        IJwtService jwtService)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _jwtService = jwtService;
    }

    /// <summary>
    /// Register a new user account.
    /// </summary>
    /// <param name="request">Registration details including email, password, and optional display name.</param>
    /// <returns>JWT token and user information.</returns>
    /// <response code="200">Successfully registered and authenticated.</response>
    /// <response code="400">Email already registered or validation failed.</response>
    [HttpPost("register")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register([FromBody] RegisterRequest request)
    {
        var existingUser = await _userManager.FindByEmailAsync(request.Email);
        if (existingUser != null)
            return BadRequest(ApiResponse<AuthResponse>.Fail("Email already registered"));

        var user = new ApplicationUser
        {
            UserName = request.Email,
            Email = request.Email,
            DisplayName = request.DisplayName
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
            return BadRequest(ApiResponse<AuthResponse>.Fail(result.Errors.Select(e => e.Description)));

        var token = _jwtService.GenerateToken(user.Id, user.Email!);
        return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                DisplayName = user.DisplayName
            }
        }));
    }

    /// <summary>
    /// Authenticate a user and obtain a JWT token.
    /// </summary>
    /// <param name="request">Login credentials (email and password).</param>
    /// <returns>JWT token and user information.</returns>
    /// <response code="200">Successfully authenticated.</response>
    /// <response code="401">Invalid email or password.</response>
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AuthResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login([FromBody] LoginRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null)
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Invalid email or password"));

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
        if (!result.Succeeded)
            return Unauthorized(ApiResponse<AuthResponse>.Fail("Invalid email or password"));

        var token = _jwtService.GenerateToken(user.Id, user.Email!);
        return Ok(ApiResponse<AuthResponse>.Ok(new AuthResponse
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Email = user.Email!,
                DisplayName = user.DisplayName
            }
        }));
    }

    /// <summary>
    /// Get the currently authenticated user's profile.
    /// </summary>
    /// <returns>User profile information.</returns>
    /// <response code="200">User profile retrieved successfully.</response>
    /// <response code="401">User not authenticated.</response>
    /// <response code="404">User not found.</response>
    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                     ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);

        if (string.IsNullOrEmpty(userId))
            return Unauthorized(ApiResponse<UserDto>.Fail("User not found"));

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
            return NotFound(ApiResponse<UserDto>.Fail("User not found"));

        return Ok(ApiResponse<UserDto>.Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email!,
            DisplayName = user.DisplayName
        }));
    }
}

internal static class JwtRegisteredClaimNames
{
    public const string Sub = "sub";
}
