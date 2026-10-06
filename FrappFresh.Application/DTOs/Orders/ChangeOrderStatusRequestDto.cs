using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.DTOs.Orders;

public class ChangeOrderStatusRequestDto
{
    public OrderStatus Status { get; set; }
}
