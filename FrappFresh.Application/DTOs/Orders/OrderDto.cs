using FrappFresh.Application.DTOs.Payments;
using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.DTOs.Orders;

public record OrderDto(Guid Id, string OrderNumber, Guid UserId, string CustomerName, string CustomerPhone,
    DeliveryType DeliveryType, string? DeliveryAddress, string? Notes, OrderStatus Status,
    decimal Subtotal, decimal DeliveryFee, decimal Total, IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<PaymentDto> Payments, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
