using System.IdentityModel.Tokens.Jwt;
using System.Text;
using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Interfaces;
using FrappFresh.Infrastructure.Options;
using FrappFresh.Infrastructure.Persistence;
using FrappFresh.Infrastructure.Persistence.Seed;
using FrappFresh.Infrastructure.Repositories;
using FrappFresh.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace FrappFresh.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<DatabaseOptions>().Bind(configuration.GetSection(DatabaseOptions.SectionName))
            .ValidateDataAnnotations().Validate(x => Version.TryParse(x.ServerVersion, out _), "Versión MySQL inválida.")
            .ValidateOnStart();
        services.AddOptions<JwtOptions>().Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .Validate(x => Encoding.UTF8.GetByteCount(x.SigningKey) >= 32, "La clave JWT requiere al menos 32 bytes.")
            .ValidateOnStart();
        services.AddOptions<SeedOptions>().Bind(configuration.GetSection(SeedOptions.SectionName))
            .Validate(x => !x.Enabled || (x.AdminPassword.Length >= 12 && x.CustomerPassword.Length >= 12),
                "Los seeds requieren contraseñas de al menos 12 caracteres en los secretos locales.")
            .Validate(x => !x.Enabled || (!string.IsNullOrWhiteSpace(x.AdminEmail) && !string.IsNullOrWhiteSpace(x.CustomerEmail)
                && !x.AdminEmail.Trim().Equals(x.CustomerEmail.Trim(), StringComparison.OrdinalIgnoreCase)),
                "Los correos de prueba deben ser diferentes y no estar vacíos.")
            .ValidateOnStart();

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Configura ConnectionStrings:DefaultConnection.");
        services.AddDbContext<AppDbContext>((provider, builder) =>
        {
            var database = provider.GetRequiredService<IOptions<DatabaseOptions>>().Value;
            builder.UseMySql(connectionString, new MySqlServerVersion(Version.Parse(database.ServerVersion)),
                mysql => mysql.CommandTimeout(database.CommandTimeoutSeconds));
        });

        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IFrappeRepository, FrappeRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IPasswordService, PasswordService>();
        services.AddScoped<ITokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddHttpContextAccessor();
        services.TryAddScoped<ICurrentUser, CurrentUserAccessor>();
        services.AddScoped<DatabaseSeeder>();
        services.TryAddSingleton(TimeProvider.System);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer();
        services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
            .Configure<IOptions<JwtOptions>>((bearer, jwt) =>
            {
                bearer.MapInboundClaims = false;
                bearer.TokenValidationParameters = JwtTokenService.CreateValidationParameters(jwt.Value);
                bearer.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async validation =>
                    {
                        var subject = validation.Principal?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
                        if (!Guid.TryParse(subject, out var userId))
                        {
                            validation.Fail("Usuario inválido.");
                            return;
                        }
                        var db = validation.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
                        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId,
                            validation.HttpContext.RequestAborted);
                        if (user is null || !user.IsActive ||
                            validation.Principal?.FindFirst("role")?.Value != user.Role.ToString())
                            validation.Fail("Usuario inactivo o permisos modificados.");
                    }
                };
            });
        services.AddAuthorization();
        return services;
    }
}
