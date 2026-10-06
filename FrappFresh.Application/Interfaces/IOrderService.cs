using FrappFresh.Application.DTOs.Orders;

namespace FrappFresh.Application.Interfaces;

public interface IOrderService
{
    Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OrderDto>> GetMineAsync(CancellationToken cancellationToken = default);
    Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OrderDto> CreateAsync(OrderRequestDto request, CancellationToken cancellationToken = default);
    Task<OrderDto> UpdateAsync(Guid id, OrderRequestDto request, CancellationToken cancellationToken = default);
    Task<OrderDto> ChangeStatusAsync(Guid id, ChangeOrderStatusRequestDto request, CancellationToken cancellationToken = default);
    Task<OrderDto> CancelAsync(Guid id, CancellationToken cancellationToken = default);
}
