using FrappFresh.Application.DTOs.Payments;

namespace FrappFresh.Application.Interfaces;

public interface IPaymentService
{
    Task<IReadOnlyList<PaymentDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PaymentDto>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<PaymentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentDto> CreateAsync(CreatePaymentRequestDto request, CancellationToken cancellationToken = default);
    Task<PaymentDto> ConfirmCashAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PaymentDto> CancelAsync(Guid id, CancelPaymentRequestDto request, CancellationToken cancellationToken = default);
}
