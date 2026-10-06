using FrappFresh.Application;
using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Enums;
using FrappFresh.Infrastructure;
using FrappFresh.Infrastructure.Persistence;
using FrappFresh.Tests.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace FrappFresh.Tests.Services;

internal sealed class ApplicationTestHost : IAsyncDisposable
{
    public ServiceProvider Provider { get; }

    public ApplicationTestHost(MySqlFixture database, Action<IServiceCollection>? configure = null)
    {
        var jwt = SecurityTests.JwtSettings();
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = database.ConnectionString,
            ["Jwt:Issuer"] = jwt.Issuer, ["Jwt:Audience"] = jwt.Audience, ["Jwt:SigningKey"] = jwt.SigningKey,
            ["Orders:DeliveryFee"] = "3500"
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IHostEnvironment>(new TestEnvironment());
        services.AddApplication(configuration);
        services.AddInfrastructure(configuration);
        services.AddScoped<TestCurrentUser>();
        services.AddScoped<ICurrentUser>(provider => provider.GetRequiredService<TestCurrentUser>());
        configure?.Invoke(services);
        Provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    public AsyncServiceScope Scope(Guid? actorId = null)
    {
        var scope = Provider.CreateAsyncScope();
        scope.ServiceProvider.GetRequiredService<TestCurrentUser>().UserId = actorId;
        return scope;
    }

    public async Task<User> AddUserAsync(UserRole role = UserRole.Customer)
    {
        await using var scope = Scope();
        var user = new User { FirstName = "Test", LastName = "User", Email = $"{Guid.NewGuid():N}@test.local",
            PhoneNumber = "573001234567", Role = role };
        user.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordService>().HashPassword(user, "PasswordDePrueba123!");
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    public async Task<Frappe> AddFrappeAsync(decimal price = 12000m)
    {
        await using var scope = Scope();
        var frappe = new Frappe { Name = "Fresa", Flavor = "Fresa", Description = "Fresa natural", Price = price };
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Frappes.Add(frappe);
        await db.SaveChangesAsync();
        return frappe;
    }

    public ValueTask DisposeAsync() => Provider.DisposeAsync();

    private sealed class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";
        public string ApplicationName { get; set; } = "FrappFresh.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}

internal sealed class TestCurrentUser : ICurrentUser
{
    public Guid? UserId { get; set; }
}
