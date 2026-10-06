using FrappFresh.Application.DTOs.Payments;
using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Enums;
using FrappFresh.Domain.Interfaces;

namespace FrappFresh.Application.Services;

public class PaymentService(IPaymentRepository payments, IOrderRepository orders, IUserAccessService access,
    IRequestValidator validator, IUnitOfWork unitOfWork, TimeProvider clock) : IPaymentService
{
    public async Task<IReadOnlyList<PaymentDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await access.RequireAdminAsync(cancellationToken);
        return (await payments.GetAllAsync(cancellationToken)).Select(x => x.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<PaymentDto>> GetByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(orderId);
        await access.RequireUserAsync(cancellationToken);
        var order = await FindOrderAsync(orderId, cancellationToken);
        await access.RequireOwnerOrAdminAsync(order.UserId, cancellationToken);
        return (await payments.GetByOrderIdAsync(orderId, cancellationToken)).Select(x => x.ToDto()).ToArray();
    }

    public async Task<PaymentDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await access.RequireUserAsync(cancellationToken);
        var payment = await FindAsync(id, cancellationToken);
        var order = await FindOrderAsync(payment.OrderId, cancellationToken);
        await access.RequireOwnerOrAdminAsync(order.UserId, cancellationToken);
        return payment.ToDto();
    }

    public async Task<PaymentDto> CreateAsync(CreatePaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireUserAsync(ct);
            var order = await FindOrderAsync(request.OrderId, ct);
            await access.RequireOwnerOrAdminAsync(order.UserId, ct);
            EnsureOpenOrder(order);
            if (order.Payments.Any(x => x.Status == PaymentStatus.Approved))
                throw new BusinessRuleException("El pedido ya está pagado.");
            var pending = order.Payments.FirstOrDefault(x => x.Status == PaymentStatus.Pending);
            if (pending is not null)
            {
                if (pending.Method == request.Method && pending.Amount == order.Total && pending.Currency == "COP")
                    return pending.ToDto();
                throw new ConflictException("Ya existe un pago pendiente para este pedido.");
            }
            var payment = new Payment
            {
                OrderId = order.Id, Amount = order.Total, Currency = "COP", Method = request.Method,
                Status = PaymentStatus.Pending, PaymentReference = $"PAY-{Guid.NewGuid():N}"
            };
            // Crear este registro no cobra ni aprueba un pago electrónico.
            await payments.AddAsync(payment, ct);
            return payment.ToDto();
        }, cancellationToken);
    }

    public async Task<PaymentDto> ConfirmCashAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireAdminAsync(ct);
            var payment = await FindAsync(id, ct);
            if (payment.Method != PaymentMethod.Cash)
                throw new BusinessRuleException("Los pagos electrónicos solo se confirman mediante la integración de la pasarela.");
            if (payment.Status == PaymentStatus.Approved) return payment.ToDto();
            if (payment.Status != PaymentStatus.Pending) throw new BusinessRuleException("El pago no está pendiente.");
            var order = await FindOrderAsync(payment.OrderId, ct);
            EnsureOpenOrder(order);
            if (payment.Amount != order.Total || payment.Currency != "COP")
                throw new BusinessRuleException("El monto del pago no coincide con el pedido.");
            if (order.Payments.Any(x => x.Id != payment.Id && x.Status == PaymentStatus.Approved))
                throw new BusinessRuleException("El pedido ya está pagado.");
            payment.Status = PaymentStatus.Approved;
            payment.PaidAt = clock.GetUtcNow();
            payment.FailureReason = null;
            await payments.UpdateAsync(payment, ct);
            return (await FindAsync(id, ct)).ToDto();
        }, cancellationToken);
    }

    public async Task<PaymentDto> CancelAsync(Guid id, CancelPaymentRequestDto request, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireUserAsync(ct);
            var payment = await FindAsync(id, ct);
            var order = await FindOrderAsync(payment.OrderId, ct);
            await access.RequireOwnerOrAdminAsync(order.UserId, ct);
            if (payment.Status == PaymentStatus.Cancelled) return payment.ToDto();
            if (payment.Status != PaymentStatus.Pending)
                throw new BusinessRuleException("Solo se pueden cancelar intentos de pago pendientes.");
            if (payment.Provider is not null)
                throw new BusinessRuleException("El pago debe cancelarse mediante la integración de su pasarela.");
            payment.Status = PaymentStatus.Cancelled;
            payment.FailureReason = request.Reason.Trim();
            await payments.UpdateAsync(payment, ct);
            return (await FindAsync(id, ct)).ToDto();
        }, cancellationToken);
    }

    private static void EnsureOpenOrder(Order order)
    {
        if (order.Status is OrderStatus.Cancelled or OrderStatus.Completed)
            throw new BusinessRuleException("El pedido ya está cerrado.");
        if (order.Total <= 0) throw new BusinessRuleException("El pedido no tiene un total válido.");
    }

    private async Task<Payment> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await payments.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Pago no encontrado.");

    private async Task<Order> FindOrderAsync(Guid id, CancellationToken cancellationToken) =>
        await orders.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Pedido no encontrado.");
}
