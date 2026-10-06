using System.Net;
using System.Net.Http.Headers;
using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Entities;
using FrappFresh.Infrastructure;
using FrappFresh.Infrastructure.Options;
using FrappFresh.Infrastructure.Persistence;
using FrappFresh.Infrastructure.Persistence.Seed;
using FrappFresh.Infrastructure.Security;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FrappFresh.Tests.Infrastructure;

[Collection("MySql")]
public class SeedAndAuthenticationTests(MySqlFixture database)
{
    [MySqlFact]
    public async Task Seeds_AreRepeatable_KeepExistingChanges_AndNeverRunInProduction()
    {
        await using var context = database.CreateContext();
        var passwords = new PasswordService();
        var settings = Options.Create(new SeedOptions
        {
            Enabled = true, AdminPassword = "TestAdminPassword123!", CustomerPassword = "TestCustomerPassword123!"
        });
        var tokens = new JwtTokenService(Options.Create(SecurityTests.JwtSettings()), TimeProvider.System);
        var seeder = new DatabaseSeeder(context, passwords, tokens, settings, new TestEnvironment());
        await seeder.SeedAsync();
        var counts = new[] { await context.Users.CountAsync(), await context.Frappes.CountAsync(),
            await context.Orders.CountAsync(), await context.Payments.CountAsync(), await context.RefreshTokens.CountAsync() };
        var admin = await context.Users.SingleAsync(x => x.Email == settings.Value.AdminEmail);
        Assert.True(passwords.VerifyPassword(admin, settings.Value.AdminPassword));
        var originalHash = admin.PasswordHash;
        var frappe = await context.Frappes.SingleAsync(x => x.Id == Guid.Parse("20000000-0000-0000-0000-000000000001"));
        frappe.Name = "Cambio manual";
        await context.SaveChangesAsync();

        await seeder.SeedAsync();
        Assert.Equal(counts, new[] { await context.Users.CountAsync(), await context.Frappes.CountAsync(),
            await context.Orders.CountAsync(), await context.Payments.CountAsync(), await context.RefreshTokens.CountAsync() });
        Assert.Equal(originalHash, admin.PasswordHash);
        Assert.Equal("Cambio manual", frappe.Name);
        var productionSeeder = new DatabaseSeeder(context, passwords, tokens, settings,
            new TestEnvironment { EnvironmentName = Environments.Production });
        await Assert.ThrowsAsync<InvalidOperationException>(() => productionSeeder.SeedAsync());
    }

    [MySqlFact]
    public async Task BearerMiddleware_EnforcesAuthenticationRolesAndUserDeactivation()
    {
        var jwt = SecurityTests.JwtSettings();
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Testing" });
        builder.WebHost.UseTestServer();
        builder.Logging.SetMinimumLevel(LogLevel.Warning);
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = database.ConnectionString,
            ["Jwt:Issuer"] = jwt.Issuer,
            ["Jwt:Audience"] = jwt.Audience,
            ["Jwt:SigningKey"] = jwt.SigningKey
        });
        builder.Services.AddInfrastructure(builder.Configuration);
        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/private", () => "ok").RequireAuthorization();
        app.MapGet("/admin", () => "ok").RequireAuthorization(policy => policy.RequireRole("Admin"));
        await app.StartAsync();

        using var client = app.GetTestClient();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/private")).StatusCode);
        await using var scope = app.Services.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var user = new User { Email = $"{Guid.NewGuid():N}@test.local" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var pair = await scope.ServiceProvider.GetRequiredService<IRefreshTokenService>().CreateAsync(user.Id);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", pair.AccessToken);
        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/private")).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await client.GetAsync("/admin")).StatusCode);

        user.IsActive = false;
        await context.SaveChangesAsync();
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/private")).StatusCode);
        await app.StopAsync();
    }

    private class TestEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "FrappFresh.Tests";
        public string ContentRootPath { get; set; } = Directory.GetCurrentDirectory();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
