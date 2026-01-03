namespace NameMatch.Application.DTOs.Auth;

public class AuthResponse
{
    public required string Token { get; set; }
    public required UserDto User { get; set; }
}

public class UserDto
{
    public required string Id { get; set; }
    public required string Email { get; set; }
    public string? DisplayName { get; set; }
}
