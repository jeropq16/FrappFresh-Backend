namespace FrappFresh.Application.Security;

public record AccessTokenResult(string Token, DateTimeOffset ExpiresAt);
