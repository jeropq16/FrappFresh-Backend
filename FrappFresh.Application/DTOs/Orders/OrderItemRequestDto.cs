namespace FrappFresh.Application.DTOs.Orders;

public class OrderItemRequestDto
{
    public Guid FrappeId { get; set; }
    public int Quantity { get; set; }
    public string? Notes { get; set; }
}
