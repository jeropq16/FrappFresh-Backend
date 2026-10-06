using FrappFresh.Infrastructure.Options;
using FrappFresh.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FrappFresh.Infrastructure.Persistence;

public static class DatabaseInitialization
{
    public static async Task InitializeDatabaseAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var provider = scope.ServiceProvider;
        var seed = provider.GetRequiredService<IOptions<SeedOptions>>().Value;
        if (seed.Enabled && !provider.GetRequiredService<IHostEnvironment>().IsDevelopment())
            throw new InvalidOperationException("Desactiva Seed:Enabled fuera de Development.");

        var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
        if (database.ApplyMigrationsOnStartup)
            await provider.GetRequiredService<AppDbContext>().Database.MigrateAsync(cancellationToken);
        if (seed.Enabled)
            await provider.GetRequiredService<DatabaseSeeder>().SeedAsync(cancellationToken);
    }
}
