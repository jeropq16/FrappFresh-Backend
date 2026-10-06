using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Enums;
using FrappFresh.Infrastructure.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace FrappFresh.Infrastructure.Persistence.Seed;

public class DatabaseSeeder(AppDbContext context, IPasswordService passwords, ITokenService tokens,
    IOptions<SeedOptions> options, IHostEnvironment environment)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var settings = options.Value;
        if (!settings.Enabled) return;
        if (!environment.IsDevelopment())
            throw new InvalidOperationException("Los datos de prueba solo se pueden cargar en Development.");

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        await AddUserAsync(settings.AdminEmail, settings.AdminPassword, "Admin", UserRole.Admin, cancellationToken);
        var customer = await AddUserAsync(settings.CustomerEmail, settings.CustomerPassword, "Cliente", UserRole.Customer, cancellationToken);

        var frappes = new[]
        {
            CreateFrappe(1, "Mango Biche Criminal", "Mango biche", "Mango biche, limón y sal.", BeverageBase.Water, 10000, false),
            CreateFrappe(2, "Fresa Sin Culpa", "Fresa", "Fresa cremosa y leche condensada.", BeverageBase.Milk, 12000, false),
            CreateFrappe(3, "Mandarina Encendida", "Mandarina", "Mandarina natural con hielo frappé.", BeverageBase.Water, 10000, false),
            CreateFrappe(4, "Milo Ghetto", "Milo", "Milo, leche, helado y crema.", BeverageBase.Milk, 15000, true),
            CreateFrappe(5, "Maracumango Flow", "Maracuyá y mango", "Mezcla tropical de maracuyá y mango.", BeverageBase.Water, 11000, false),
            CreateFrappe(6, "Mora con Helado", "Mora", "Mora, leche y helado cremoso.", BeverageBase.Milk, 15000, true)
        };
        foreach (var frappe in frappes)
            if (!await context.Frappes.AnyAsync(x => x.Id == frappe.Id, cancellationToken))
                context.Frappes.Add(frappe);
        await context.SaveChangesAsync(cancellationToken);

        await AddOrderAsync("DEMO-001", customer, frappes[0].Id, DeliveryType.Pickup, true, cancellationToken);
        await AddOrderAsync("DEMO-002", customer, frappes[3].Id, DeliveryType.Delivery, false, cancellationToken);

        var tokenId = Guid.Parse("40000000-0000-0000-0000-000000000001");
        if (!await context.RefreshTokens.AnyAsync(x => x.Id == tokenId, cancellationToken))
        {
            context.RefreshTokens.Add(new RefreshToken
            {
                Id = tokenId,
                UserId = customer.Id,
                TokenHash = tokens.HashRefreshToken(tokens.GenerateRefreshToken()),
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                RevokedAt = DateTimeOffset.UtcNow.AddDays(-2)
            });
        }
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task<User> AddUserAsync(string email, string password, string name, UserRole role, CancellationToken cancellationToken)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var existing = await context.Users.SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
        if (existing is not null) return existing;

        var user = new User
        {
            FirstName = name, LastName = "Prueba", Email = normalizedEmail,
            PhoneNumber = "570000000000", Role = role
        };
        user.PasswordHash = passwords.HashPassword(user, password);
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
        return user;
    }

    private static Frappe CreateFrappe(int number, string name, string flavor, string description,
        BeverageBase beverageBase, decimal price, bool premium) => new()
    {
        Id = Guid.Parse($"20000000-0000-0000-0000-{number:D12}"),
        Name = name, Flavor = flavor, Description = description, Base = beverageBase,
        Price = price, IsPremium = premium, Toppings = premium ? "Crema y topping crocante" : null
    };

    private async Task AddOrderAsync(string number, User customer, Guid frappeId, DeliveryType deliveryType, bool paid,
        CancellationToken cancellationToken)
    {
        if (await context.Orders.AnyAsync(x => x.OrderNumber == number, cancellationToken)) return;
        var frappe = await context.Frappes.SingleAsync(x => x.Id == frappeId, cancellationToken);
        var subtotal = frappe.Price * 2;
        var deliveryFee = deliveryType == DeliveryType.Delivery ? 3000m : 0m;
        var order = new Order
        {
            OrderNumber = number, UserId = customer.Id,
            CustomerName = $"{customer.FirstName} {customer.LastName}", CustomerPhone = customer.PhoneNumber,
            DeliveryType = deliveryType, DeliveryAddress = deliveryType == DeliveryType.Delivery ? "Dirección ficticia de prueba, Medellín" : null,
            Status = paid ? OrderStatus.Completed : OrderStatus.Pending,
            Subtotal = subtotal, DeliveryFee = deliveryFee, Total = subtotal + deliveryFee,
            Notes = "Pedido ficticio para desarrollo."
        };
        order.Items.Add(new OrderItem
        {
            FrappeId = frappe.Id, FrappeName = frappe.Name, Flavor = frappe.Flavor, Toppings = frappe.Toppings,
            Quantity = 2, UnitPrice = frappe.Price, Total = subtotal
        });
        order.Payments.Add(new Payment
        {
            Amount = order.Total, Method = PaymentMethod.Cash,
            Status = paid ? PaymentStatus.Approved : PaymentStatus.Pending,
            PaymentReference = number, PaidAt = paid ? DateTimeOffset.UtcNow : null
        });
        context.Orders.Add(order);
        await context.SaveChangesAsync(cancellationToken);
    }
}
