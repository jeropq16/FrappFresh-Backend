using FrappFresh.Application.Interfaces;
using FrappFresh.Application.Security;
using FrappFresh.Domain.Entities;
using FrappFresh.Infrastructure.Options;
using FrappFresh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace FrappFresh.Infrastructure.Security;

public class RefreshTokenService(AppDbContext context, ITokenService tokens, IOptions<JwtOptions> options,
    TimeProvider clock) : IRefreshTokenService
{
    public async Task<TokenPair> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await context.Users.SingleOrDefaultAsync(x => x.Id == userId && x.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Usuario no disponible.");
        var (entity, pair) = CreatePair(user);
        context.RefreshTokens.Add(entity);
        await context.SaveChangesAsync(cancellationToken);
        return pair;
    }

    public async Task<TokenPair?> RotateAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 256) return null;
        var hash = tokens.HashRefreshToken(refreshToken);
        var current = await context.RefreshTokens.Include(x => x.User)
            .SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        var now = clock.GetUtcNow();
        if (current is null || current.RevokedAt is not null || current.ExpiresAt <= now || !current.User.IsActive)
            return null;

        var (replacement, pair) = CreatePair(current.User);
        current.RevokedAt = now;
        current.ReplacedByTokenHash = replacement.TokenHash;
        context.RefreshTokens.Add(replacement);
        try
        {
            // EF guarda la revocación y el nuevo token en la misma transacción.
            // RevokedAt es un concurrency token: dos renovaciones no pueden consumir el mismo token.
            await context.SaveChangesAsync(cancellationToken);
            return pair;
        }
        catch (DbUpdateConcurrencyException)
        {
            context.Entry(replacement).State = EntityState.Detached;
            await context.Entry(current).ReloadAsync(cancellationToken);
            return null;
        }
    }

    public async Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || refreshToken.Length > 256) return;
        var hash = tokens.HashRefreshToken(refreshToken);
        var now = clock.GetUtcNow();
        await context.RefreshTokens.Where(x => x.TokenHash == hash && x.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.RevokedAt, now)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);

        // ExecuteUpdate no actualiza el ChangeTracker.
        foreach (var entry in context.ChangeTracker.Entries<RefreshToken>().Where(x => x.Entity.TokenHash == hash).ToList())
            await entry.ReloadAsync(cancellationToken);
    }

    public async Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var now = clock.GetUtcNow();
        await context.RefreshTokens.Where(x => x.UserId == userId && x.RevokedAt == null)
            .ExecuteUpdateAsync(update => update.SetProperty(x => x.RevokedAt, now)
                .SetProperty(x => x.UpdatedAt, now), cancellationToken);
        foreach (var entry in context.ChangeTracker.Entries<RefreshToken>().Where(x => x.Entity.UserId == userId).ToList())
            await entry.ReloadAsync(cancellationToken);
    }

    private (RefreshToken Entity, TokenPair Pair) CreatePair(User user)
    {
        var rawToken = tokens.GenerateRefreshToken();
        var expiresAt = clock.GetUtcNow().AddDays(options.Value.RefreshTokenDays);
        var accessToken = tokens.CreateAccessToken(user);
        var entity = new RefreshToken
        {
            UserId = user.Id,
            TokenHash = tokens.HashRefreshToken(rawToken),
            ExpiresAt = expiresAt
        };
        return (entity, new TokenPair(accessToken.Token, accessToken.ExpiresAt, rawToken, expiresAt));
    }
}
