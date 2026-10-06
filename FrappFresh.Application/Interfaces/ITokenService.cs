using FrappFresh.Application.Security;
using FrappFresh.Domain.Entities;

namespace FrappFresh.Application.Interfaces;

public interface ITokenService
{
    AccessTokenResult CreateAccessToken(User user);
    string GenerateRefreshToken();
    string HashRefreshToken(string token);
}
