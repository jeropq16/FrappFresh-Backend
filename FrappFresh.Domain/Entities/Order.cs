using FrappFresh.Domain.Enums;

namespace FrappFresh.Domain.Entities;

public class Order : Entity
{
    public string OrderNumber { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public DeliveryType DeliveryType { get; set; } = DeliveryType.Pickup;
    public string? DeliveryAddress { get; set; }
    public string? Notes { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    public decimal Subtotal { get; set; }
    public decimal DeliveryFee { get; set; }
    public decimal Total { get; set; }

    public User User { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
