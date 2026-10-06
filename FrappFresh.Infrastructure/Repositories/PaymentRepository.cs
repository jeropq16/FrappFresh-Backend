using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Interfaces;
using FrappFresh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FrappFresh.Infrastructure.Repositories;

public class PaymentRepository(AppDbContext context) : Repository<Payment>(context), IPaymentRepository
{
    public async Task<IReadOnlyList<Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().Where(x => x.OrderId == orderId)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);

    public Task<Payment?> GetByProviderTransactionIdAsync(string provider, string transactionId,
        CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(x => x.Provider == provider && x.ProviderTransactionId == transactionId,
            cancellationToken);
}
