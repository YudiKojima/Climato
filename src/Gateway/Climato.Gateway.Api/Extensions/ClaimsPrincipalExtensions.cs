using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace Climato.Gateway.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid GetUserId(this ClaimsPrincipal principal)
    {
        return Guid.Parse(principal.FindFirstValue(JwtRegisteredClaimNames.Sub)!);
    }
}
