using Climato.Gateway.Application.Model.Users;

namespace Climato.Gateway.Application.Model.Auth;

public class AuthDto
{
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public UserDto User { get; set; } = new();
}
