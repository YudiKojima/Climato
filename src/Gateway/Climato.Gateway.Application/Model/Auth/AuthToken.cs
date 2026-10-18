namespace Climato.Gateway.Application.Model.Auth;

public sealed record AuthToken(string Token, DateTimeOffset ExpiresAt);
