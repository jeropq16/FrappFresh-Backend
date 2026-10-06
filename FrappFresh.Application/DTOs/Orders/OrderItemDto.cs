namespace FrappFresh.Application.DTOs.Orders;

public record OrderItemDto(Guid Id, Guid OrderId, Guid FrappeId, string FrappeName, string Flavor,
    string? Toppings, int Quantity, decimal UnitPrice, decimal Total, string? Notes,
    DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
