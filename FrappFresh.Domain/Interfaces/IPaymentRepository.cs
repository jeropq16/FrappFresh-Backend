using FrappFresh.Domain.Entities;

namespace FrappFresh.Domain.Interfaces;

public interface IPaymentRepository : IRepository<Payment>
{
    Task<IReadOnlyList<Payment>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Payment?> GetByProviderTransactionIdAsync(string provider, string transactionId,
        CancellationToken cancellationToken = default);
}
