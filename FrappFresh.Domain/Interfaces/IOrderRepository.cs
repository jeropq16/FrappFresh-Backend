using FrappFresh.Domain.Entities;

namespace FrappFresh.Domain.Interfaces;

public interface IOrderRepository : IRepository<Order>
{
    // El pedido se guarda junto con sus Items.
    Task<IReadOnlyList<Order>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
