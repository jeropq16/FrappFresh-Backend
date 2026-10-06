using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.DTOs.Payments;

// No acepta monto, estado ni referencias de la pasarela desde el cliente.
public class CreatePaymentRequestDto
{
    public Guid OrderId { get; set; }
    public PaymentMethod Method { get; set; }
}
