using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.DTOs.Orders;

// El usuario, los precios, el domicilio y los totales los determina el servidor.
public class OrderRequestDto
{
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DeliveryType DeliveryType { get; set; } = DeliveryType.Pickup;
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public List<OrderItemRequestDto> Items { get; set; } = [];
}
