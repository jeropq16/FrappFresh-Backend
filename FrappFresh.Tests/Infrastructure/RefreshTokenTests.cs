using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Entities;
using FrappFresh.Infrastructure.Persistence;
using FrappFresh.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Options;

namespace FrappFresh.Tests.Infrastructure;

[Collection("MySql")]
public class RefreshTokenTests(MySqlFixture database)
{
    private static ITokenService Tokens(TimeProvider? clock = null) =>
        new JwtTokenService(Options.Create(SecurityTests.JwtSettings()), clock ?? TimeProvider.System);

    private static RefreshTokenService Service(AppDbContext context, TimeProvider? clock = null) =>
        new(context, Tokens(clock), Options.Create(SecurityTests.JwtSettings()), clock ?? TimeProvider.System);

    private static async Task<User> CreateUser(AppDbContext context)
    {
        var user = new User { Email = $"{Guid.NewGuid():N}@test.local" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    [MySqlFact]
    public async Task Rotation_StoresOnlyHashes_RevokesPreviousToken_AndLogoutPreventsReuse()
    {
        await using var context = database.CreateContext();
        var user = await CreateUser(context);
        var service = Service(context);
        var original = await service.CreateAsync(user.Id);
        var originalHash = Tokens().HashRefreshToken(original.RefreshToken);
        var stored = await context.RefreshTokens.SingleAsync(x => x.TokenHash == originalHash);
        Assert.NotEqual(original.RefreshToken, stored.TokenHash);

        var replacement = await service.RotateAsync(original.RefreshToken);
        Assert.NotNull(replacement);
        Assert.NotEqual(original.RefreshToken, replacement.RefreshToken);
        Assert.NotNull(stored.RevokedAt);
        Assert.Equal(Tokens().HashRefreshToken(replacement.RefreshToken), stored.ReplacedByTokenHash);
        Assert.Null(await service.RotateAsync(original.RefreshToken));

        await service.RevokeAsync(replacement.RefreshToken);
        await service.RevokeAsync(replacement.RefreshToken); // Logout repetido es idempotente.
        Assert.Null(await service.RotateAsync(replacement.RefreshToken));
    }

    [MySqlFact]
    public async Task ExpiredUnknownAndInactiveUserTokens_CannotBeRenewed()
    {
        await using var context = database.CreateContext();
        var user = await CreateUser(context);
        var now = DateTimeOffset.UtcNow;
        var original = await Service(context, new TestClock(now)).CreateAsync(user.Id);
        Assert.Null(await Service(context, new TestClock(now.AddDays(8))).RotateAsync(original.RefreshToken));
        Assert.Null(await Service(context).RotateAsync("unknown"));

        user.IsActive = false;
        await context.SaveChangesAsync();
        Assert.Null(await Service(context).RotateAsync(original.RefreshToken));
        await Assert.ThrowsAsync<InvalidOperationException>(() => Service(context).CreateAsync(user.Id));
    }

    [MySqlFact]
    public async Task ConcurrentRotations_AllowOnlyOneReplacement()
    {
        await using var setup = database.CreateContext();
        var user = await CreateUser(setup);
        var original = await Service(setup).CreateAsync(user.Id);

        var barrier = new RotationBarrier();
        await using var firstContext = database.CreateContext(barrier);
        await using var secondContext = database.CreateContext(barrier);
        var results = await Task.WhenAll(
            Service(firstContext).RotateAsync(original.RefreshToken),
            Service(secondContext).RotateAsync(original.RefreshToken));

        Assert.Single(results, x => x is not null);
        await using var verification = database.CreateContext();
        Assert.Equal(2, await verification.RefreshTokens.CountAsync(x => x.UserId == user.Id));
        Assert.Equal(1, await verification.RefreshTokens.CountAsync(x => x.UserId == user.Id && x.RevokedAt == null));
    }

    private sealed class RotationBarrier : SaveChangesInterceptor
    {
        private int _arrivals;
        private readonly TaskCompletionSource _ready = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<RefreshToken>().Any(x => x.State == EntityState.Modified))
            {
                if (Interlocked.Increment(ref _arrivals) == 2) _ready.TrySetResult();
                await _ready.Task.WaitAsync(TimeSpan.FromSeconds(10), cancellationToken);
            }
            return result;
        }
    }
}
