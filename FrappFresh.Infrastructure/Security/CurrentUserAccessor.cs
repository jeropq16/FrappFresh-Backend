using System.IdentityModel.Tokens.Jwt;
using FrappFresh.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace FrappFresh.Infrastructure.Security;

public class CurrentUserAccessor(IHttpContextAccessor context) : ICurrentUser
{
    public Guid? UserId
    {
        get
        {
            var principal = context.HttpContext?.User;
            return principal?.Identity?.IsAuthenticated == true &&
                Guid.TryParse(principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out var id) ? id : null;
        }
    }
}
