using FrappFresh.Domain.Enums;

namespace FrappFresh.Domain.Entities;

// Cada registro representa un intento de pago del pedido.
public class Payment : Entity
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "COP";
    public PaymentMethod Method { get; set; } = PaymentMethod.Cash;
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
    public string? Provider { get; set; }
    public string? ProviderTransactionId { get; set; }
    public string? PaymentReference { get; set; }
    public string? FailureReason { get; set; }
    public DateTimeOffset? PaidAt { get; set; }

    public Order Order { get; set; } = null!;
}
