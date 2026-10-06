using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Enums;
using FrappFresh.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;

namespace FrappFresh.Tests.Infrastructure;

[Collection("MySql")]
public class RepositoryTests(MySqlFixture database)
{
    [MySqlFact]
    public async Task Crud_PersistsUpdatesDeletesAndFiltersAvailableFrappes()
    {
        await using var context = database.CreateContext();
        var repository = new FrappeRepository(context);
        var frappe = new Frappe { Name = "Prueba CRUD", Flavor = "Mango", Description = "Prueba", Price = 10000 };
        await repository.AddAsync(frappe);
        var createdAt = frappe.CreatedAt;
        Assert.Contains(await repository.GetAvailableAsync(), x => x.Id == frappe.Id);

        var detached = (await repository.GetByIdAsync(frappe.Id))!;
        detached.Price = 12000;
        detached.IsAvailable = false;
        detached.CreatedAt = DateTimeOffset.MinValue;
        await repository.UpdateAsync(detached);
        context.ChangeTracker.Clear();
        var updated = (await repository.GetByIdAsync(frappe.Id))!;
        Assert.Equal(12000, updated.Price);
        Assert.NotNull(updated.UpdatedAt);
        Assert.True((createdAt - updated.CreatedAt).Duration() < TimeSpan.FromMilliseconds(1));
        Assert.DoesNotContain(await repository.GetAvailableAsync(), x => x.Id == frappe.Id);
        await repository.DeleteAsync(frappe.Id);
        Assert.Null(await repository.GetByIdAsync(frappe.Id));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => repository.DeleteAsync(frappe.Id));
    }

    [MySqlFact]
    public async Task Email_IsNormalizedAndUnique_AndQueriesAreCaseInsensitive()
    {
        var email = $"Test-{Guid.NewGuid():N}@EXAMPLE.TEST";
        await using var context = database.CreateContext();
        var repository = new UserRepository(context);
        var user = new User { Email = " " + email + " ", PasswordHash = "test-hash" };
        await repository.AddAsync(user);
        Assert.Equal(email.ToLowerInvariant(), user.Email);
        Assert.Equal(user.Id, (await repository.GetByEmailAsync(email))?.Id);
        await Assert.ThrowsAsync<DbUpdateException>(() =>
            repository.AddAsync(new User { Email = email.ToLowerInvariant(), PasswordHash = "test-hash" }));
    }

    [MySqlFact]
    public async Task OrderUpdate_AddsUpdatesRemovesLines_AndLoadsPayments()
    {
        await using var context = database.CreateContext();
        var user = new User { Email = $"{Guid.NewGuid():N}@test.local" };
        var frappe = new Frappe { Name = "Prueba", Flavor = "Mango", Price = 10000 };
        context.AddRange(user, frappe);
        await context.SaveChangesAsync();
        var order = NewOrder(user.Id, frappe.Id);
        var repository = new OrderRepository(context);
        await repository.AddAsync(order);
        var originalItemId = order.Items.Single().Id;
        context.ChangeTracker.Clear();

        var loaded = (await repository.GetByIdAsync(order.Id))!;
        Assert.Single(loaded.Items);
        loaded.Items.Single().Quantity = 2;
        loaded.Items.Single().Total = 20000;
        loaded.Items.Add(new OrderItem { FrappeId = frappe.Id, FrappeName = "Otro", Flavor = "Mango", UnitPrice = 5000, Total = 5000 });
        loaded.Subtotal = loaded.Total = 25000;
        await repository.UpdateAsync(loaded);
        context.ChangeTracker.Clear();
        loaded = (await repository.GetByIdAsync(order.Id))!;
        Assert.Equal(2, loaded.Items.Count);
        loaded.Items.Remove(loaded.Items.Single(x => x.Id == originalItemId));
        loaded.Subtotal = loaded.Total = 5000;
        await repository.UpdateAsync(loaded);
        context.ChangeTracker.Clear();
        Assert.Single((await repository.GetByIdAsync(order.Id))!.Items);
        Assert.False(await context.OrderItems.AnyAsync(x => x.Id == originalItemId));

        var payment = new Payment { OrderId = order.Id, Amount = 5000, Provider = "Fake", ProviderTransactionId = Guid.NewGuid().ToString() };
        var payments = new PaymentRepository(context);
        await payments.AddAsync(payment);
        Assert.Single((await repository.GetByIdAsync(order.Id))!.Payments);
        Assert.Equal(payment.Id, (await payments.GetByProviderTransactionIdAsync("Fake", payment.ProviderTransactionId))?.Id);
        Assert.Single(await payments.GetByOrderIdAsync(order.Id));
        Assert.Single(await repository.GetByUserIdAsync(user.Id));

        // Un producto vendido no se elimina del historial por accidente.
        await Assert.ThrowsAsync<DbUpdateException>(() => new FrappeRepository(context).DeleteAsync(frappe.Id));
    }

    [MySqlFact]
    public async Task InvalidLine_RollsBackEntireOrder()
    {
        await using var context = database.CreateContext();
        var user = new User { Email = $"{Guid.NewGuid():N}@test.local" };
        var frappe = new Frappe { Name = "Prueba", Flavor = "Mango" };
        context.AddRange(user, frappe);
        await context.SaveChangesAsync();
        var order = NewOrder(user.Id, frappe.Id);
        order.Items.Single().Quantity = 0;
        await Assert.ThrowsAsync<DbUpdateException>(() => new OrderRepository(context).AddAsync(order));
        await using var verification = database.CreateContext();
        Assert.False(await verification.Orders.AnyAsync(x => x.Id == order.Id));
    }

    [MySqlFact]
    public async Task UtcDates_RoundTripAndCanBeFilteredInMySql()
    {
        await using var context = database.CreateContext();
        var user = new User { Email = $"{Guid.NewGuid():N}@test.local" };
        context.Users.Add(user);
        await context.SaveChangesAsync();
        var expiresAt = new DateTimeOffset(2030, 1, 2, 10, 0, 0, TimeSpan.FromHours(-5));
        var token = new RefreshToken { UserId = user.Id, TokenHash = Guid.NewGuid().ToString("N").PadRight(64, '0'), ExpiresAt = expiresAt };
        var repository = new RefreshTokenRepository(context);
        await repository.AddAsync(token);
        context.ChangeTracker.Clear();
        var loaded = (await repository.GetByTokenHashAsync(token.TokenHash))!;
        Assert.Equal(TimeSpan.Zero, loaded.ExpiresAt.Offset);
        Assert.Equal(expiresAt, loaded.ExpiresAt);
        Assert.Contains(await repository.GetByUserIdAsync(user.Id), x => x.Id == token.Id);
        Assert.True(await context.RefreshTokens.AnyAsync(x => x.Id == token.Id && x.ExpiresAt > DateTimeOffset.UtcNow));
    }

    private static Order NewOrder(Guid userId, Guid frappeId)
    {
        var order = new Order { UserId = userId, OrderNumber = Guid.NewGuid().ToString("N"),
            CustomerName = "Prueba", CustomerPhone = "570000000000", Subtotal = 10000, Total = 10000 };
        order.Items.Add(new OrderItem { FrappeId = frappeId, FrappeName = "Prueba", Flavor = "Mango", UnitPrice = 10000, Total = 10000 });
        return order;
    }
}
