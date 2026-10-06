namespace FrappFresh.Application.Security;

public record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
