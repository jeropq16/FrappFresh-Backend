using System.IdentityModel.Tokens.Jwt;
using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Enums;
using FrappFresh.Infrastructure.Options;
using FrappFresh.Infrastructure.Security;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FrappFresh.Tests.Infrastructure;

public class SecurityTests
{
    internal static JwtOptions JwtSettings() => new()
    {
        Issuer = "FrappFresh.Tests",
        Audience = "FrappFresh.Tests.Client",
        SigningKey = "test-only-signing-key-with-at-least-64-characters-not-for-real-use!",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    [Fact]
    public void PasswordHash_IsSalted_VerifiesCorrectPassword_AndRejectsInvalidValues()
    {
        var service = new PasswordService();
        var user = new User();
        user.PasswordHash = service.HashPassword(user, "PasswordDePrueba123!");
        Assert.NotEqual("PasswordDePrueba123!", user.PasswordHash);
        Assert.NotEqual(user.PasswordHash, service.HashPassword(user, "PasswordDePrueba123!"));
        Assert.True(service.VerifyPassword(user, "PasswordDePrueba123!"));
        Assert.False(service.VerifyPassword(user, "incorrecta"));
        user.PasswordHash = "hash-corrupto";
        Assert.False(service.VerifyPassword(user, "PasswordDePrueba123!"));
    }

    [Fact]
    public void Jwt_ValidatesSignatureIssuerAudienceLifetimeAndRole()
    {
        var settings = JwtSettings();
        var service = new JwtTokenService(Options.Create(settings), TimeProvider.System);
        var user = new User { Email = "admin@test.local", Role = UserRole.Admin };
        var result = service.CreateAccessToken(user);
        var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        var principal = handler.ValidateToken(result.Token, JwtTokenService.CreateValidationParameters(settings), out _);
        Assert.Equal(user.Id.ToString(), principal.FindFirst("sub")?.Value);
        Assert.True(principal.IsInRole("Admin"));

        var wrongIssuer = JwtTokenService.CreateValidationParameters(settings);
        wrongIssuer.ValidIssuer = "wrong";
        Assert.Throws<SecurityTokenInvalidIssuerException>(() => handler.ValidateToken(result.Token, wrongIssuer, out _));

        var wrongAudience = JwtTokenService.CreateValidationParameters(settings);
        wrongAudience.ValidAudience = "wrong";
        Assert.Throws<SecurityTokenInvalidAudienceException>(() => handler.ValidateToken(result.Token, wrongAudience, out _));

        var wrongKey = JwtTokenService.CreateValidationParameters(settings);
        wrongKey.IssuerSigningKey = new SymmetricSecurityKey(new byte[64]);
        Assert.ThrowsAny<SecurityTokenException>(() => handler.ValidateToken(result.Token, wrongKey, out _));
    }

    [Fact]
    public void ExpiredJwtAndInactiveUsers_AreRejected()
    {
        var settings = JwtSettings();
        var service = new JwtTokenService(Options.Create(settings), new TestClock(DateTimeOffset.UtcNow.AddHours(-2)));
        var token = service.CreateAccessToken(new User());
        var handler = new JwtSecurityTokenHandler();
        Assert.Throws<SecurityTokenExpiredException>(() =>
            handler.ValidateToken(token.Token, JwtTokenService.CreateValidationParameters(settings), out _));
        Assert.Throws<InvalidOperationException>(() => service.CreateAccessToken(new User { IsActive = false }));
    }

    [Fact]
    public void RefreshTokens_AreRandom_AndStoredAsDeterministicHashes()
    {
        var service = new JwtTokenService(Options.Create(JwtSettings()), TimeProvider.System);
        var token = service.GenerateRefreshToken();
        Assert.NotEqual(token, service.GenerateRefreshToken());
        Assert.True(token.Length >= 64);
        var hash = service.HashRefreshToken(token);
        Assert.Equal(64, hash.Length);
        Assert.NotEqual(token, hash);
        Assert.Equal(hash, service.HashRefreshToken(token));
    }
}

internal class TestClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
