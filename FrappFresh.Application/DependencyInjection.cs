using FluentValidation;
using FrappFresh.Application.Interfaces;
using FrappFresh.Application.Options;
using FrappFresh.Application.Services;
using FrappFresh.Application.Validators;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FrappFresh.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<OrderOptions>().Bind(configuration.GetSection(OrderOptions.SectionName))
            .Validate(x => x.DeliveryFee >= 0 && x.DeliveryFee <= ValidationRules.MaximumAmount &&
                decimal.Round(x.DeliveryFee, 2) == x.DeliveryFee, "El costo de domicilio debe ser válido y tener máximo dos decimales.")
            .ValidateOnStart();
        services.AddValidatorsFromAssemblyContaining<RegisterRequestValidator>();
        services.AddScoped<IRequestValidator, RequestValidator>();
        services.AddScoped<IUserAccessService, UserAccessService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IFrappeService, FrappeService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.TryAddSingleton(TimeProvider.System);
        return services;
    }
}
