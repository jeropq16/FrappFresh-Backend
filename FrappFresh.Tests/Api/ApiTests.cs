using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FrappFresh.Api.Controllers;
using FrappFresh.Api.Middleware;
using FrappFresh.Application;
using FrappFresh.Application.Exceptions;
using FrappFresh.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace FrappFresh.Tests.Api;

public class ApiTests
{
    [Fact]
    public async Task Routes_ExposeHealthAndSwagger_RequireLogin_AndValidatePublicLogin()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:DefaultConnection"] = "Server=localhost;Database=Unused;",
            ["Jwt:Issuer"] = "Tests",
            ["Jwt:Audience"] = "Tests",
            ["Jwt:SigningKey"] = "Test-only-signing-key-with-at-least-32-bytes",
            ["Seed:Enabled"] = "false",
            ["Database:ApplyMigrationsOnStartup"] = "false"
        });
        builder.Services.AddApplication(builder.Configuration);
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddControllers().AddApplicationPart(typeof(AuthController).Assembly);
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();
        await using var app = builder.Build();
        app.UseSwagger();
        app.UseMiddleware<ExceptionMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        await app.StartAsync();
        using var client = app.GetTestClient();

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/api/health")).StatusCode);
        foreach (var route in new[] { "/api/users", "/api/users/me", "/api/orders", "/api/orders/mine", "/api/payments", "/api/frappes" })
            Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync(route)).StatusCode);

        var response = await client.PostAsJsonAsync("/api/auth/login", new { email = "", password = "" });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var validation = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(validation.RootElement.GetProperty("errors").TryGetProperty("Email", out _));

        var swagger = await client.GetFromJsonAsync<JsonElement>("/swagger/v1/swagger.json");
        var paths = swagger.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/auth/register", out _));
        Assert.True(paths.TryGetProperty("/api/frappes/catalog", out _));
        Assert.True(paths.TryGetProperty("/api/orders/{id}/status", out _));
        Assert.True(paths.TryGetProperty("/api/payments/{id}/confirm-cash", out _));
    }

    [Theory]
    [InlineData(400)]
    [InlineData(401)]
    [InlineData(403)]
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(500)]
    public async Task Errors_ReturnExpectedStatus_AndHideUnexpectedDetails(int status)
    {
        Exception error = status switch
        {
            400 => new BusinessRuleException("Regla de negocio"),
            401 => new UnauthorizedException(),
            403 => new ForbiddenException(),
            404 => new NotFoundException("No encontrado"),
            409 => new ConflictException("Duplicado"),
            _ => new InvalidOperationException("Detalle interno privado")
        };
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        await using var app = builder.Build();
        app.UseMiddleware<ExceptionMiddleware>();
        app.Run(context => Task.FromException(error));
        await app.StartAsync();
        using var client = app.GetTestClient();
        var response = await client.GetAsync("/");
        Assert.Equal(status, (int)response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var body = await response.Content.ReadAsStringAsync();
        if (status == 500) Assert.DoesNotContain("Detalle interno privado", body);
        else Assert.Contains(error.Message, JsonDocument.Parse(body).RootElement.GetProperty("title").GetString());
    }
}
