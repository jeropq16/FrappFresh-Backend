using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Interfaces;
using FrappFresh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FrappFresh.Infrastructure.Repositories;

public class OrderRepository(AppDbContext context) : Repository<Order>(context), IOrderRepository
{
    protected override IQueryable<Order> Query => Set.Include(x => x.Items).Include(x => x.Payments).AsSplitQuery();

    public async Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Query.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);

    // Actualiza el pedido y sincroniza sus líneas en un único SaveChanges (transacción).
    // Los pagos se modifican a través de PaymentRepository.
    public override async Task UpdateAsync(Order entity, CancellationToken cancellationToken = default)
    {
        if (entity.Items.Select(x => x.Id).Distinct().Count() != entity.Items.Count)
            throw new ArgumentException("El pedido contiene detalles duplicados.", nameof(entity));

        var existing = await Set.Include(x => x.Items)
            .SingleOrDefaultAsync(x => x.Id == entity.Id, cancellationToken)
            ?? throw new KeyNotFoundException("Pedido no encontrado.");

        Context.Entry(existing).CurrentValues.SetValues(entity);
        var itemsToRemove = existing.Items.Where(x => entity.Items.All(item => item.Id != x.Id)).ToList();
        Context.OrderItems.RemoveRange(itemsToRemove);

        foreach (var item in entity.Items)
        {
            var current = existing.Items.SingleOrDefault(x => x.Id == item.Id);
            if (current is null)
            {
                current = new OrderItem();
                Context.Entry(current).CurrentValues.SetValues(item);
                current.OrderId = existing.Id;
                existing.Items.Add(current);
                Context.OrderItems.Add(current);
            }
            else
            {
                Context.Entry(current).CurrentValues.SetValues(item);
                current.OrderId = existing.Id;
            }
        }

        await Context.SaveChangesAsync(cancellationToken);
    }
}
