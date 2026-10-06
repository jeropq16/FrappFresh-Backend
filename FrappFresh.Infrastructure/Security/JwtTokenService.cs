using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FrappFresh.Application.Interfaces;
using FrappFresh.Application.Security;
using FrappFresh.Domain.Entities;
using FrappFresh.Infrastructure.Options;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FrappFresh.Infrastructure.Security;

public class JwtTokenService(IOptions<JwtOptions> options, TimeProvider clock) : ITokenService
{
    private readonly JwtOptions _options = options.Value;

    public AccessTokenResult CreateAccessToken(User user)
    {
        if (!user.IsActive || user.Id == Guid.Empty || !Enum.IsDefined(user.Role))
            throw new InvalidOperationException("El usuario no puede iniciar sesión.");

        var now = clock.GetUtcNow();
        var expiresAt = now.AddMinutes(_options.AccessTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new Claim("role", user.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };
        var credentials = new SigningCredentials(
            new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(_options.Issuer, _options.Audience, claims,
            now.UtcDateTime, expiresAt.UtcDateTime, credentials);
        return new AccessTokenResult(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    public string HashRefreshToken(string token)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(token);
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }

    public static TokenValidationParameters CreateValidationParameters(JwtOptions options) => new()
    {
        ValidateIssuer = true,
        ValidIssuer = options.Issuer,
        ValidateAudience = true,
        ValidAudience = options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
        ValidateLifetime = true,
        RequireExpirationTime = true,
        RequireSignedTokens = true,
        ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
        ClockSkew = TimeSpan.FromSeconds(30),
        NameClaimType = JwtRegisteredClaimNames.Name,
        RoleClaimType = "role"
    };
}
