using FrappFresh.Application.DTOs.Orders;
using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using FrappFresh.Application.Options;
using FrappFresh.Application.Validators;
using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Enums;
using FrappFresh.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace FrappFresh.Application.Services;

public class OrderService(IOrderRepository orders, IFrappeRepository frappes, IPaymentRepository payments,
    IUserAccessService access, IRequestValidator validator, IUnitOfWork unitOfWork, IOptions<OrderOptions> options) : IOrderService
{
    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await access.RequireAdminAsync(cancellationToken);
        return (await orders.GetAllAsync(cancellationToken)).Select(x => x.ToDto()).ToArray();
    }

    public async Task<IReadOnlyList<OrderDto>> GetMineAsync(CancellationToken cancellationToken = default)
    {
        var user = await access.RequireUserAsync(cancellationToken);
        return (await orders.GetByUserIdAsync(user.Id, cancellationToken)).Select(x => x.ToDto()).ToArray();
    }

    public async Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await access.RequireUserAsync(cancellationToken);
        var order = await FindAsync(id, cancellationToken);
        await access.RequireOwnerOrAdminAsync(order.UserId, cancellationToken);
        return order.ToDto();
    }

    public async Task<OrderDto> CreateAsync(OrderRequestDto request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            var user = await access.RequireUserAsync(ct);
            var order = new Order { UserId = user.Id, OrderNumber = $"FF-{Guid.NewGuid():N}", Status = OrderStatus.Pending };
            await ApplyRequestAsync(order, request, ct);
            await orders.AddAsync(order, ct);
            return (await FindAsync(order.Id, ct)).ToDto();
        }, cancellationToken);
    }

    public async Task<OrderDto> UpdateAsync(Guid id, OrderRequestDto request, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireUserAsync(ct);
            var order = await FindAsync(id, ct);
            await access.RequireOwnerOrAdminAsync(order.UserId, ct);
            if (order.Status != OrderStatus.Pending)
                throw new BusinessRuleException("Solo puedes editar pedidos pendientes.");
            if (order.Payments.Any(x => x.Status is PaymentStatus.Pending or PaymentStatus.Approved))
                throw new BusinessRuleException("Cancela el intento de pago pendiente antes de editar; un pedido pagado no se puede editar.");
            await ApplyRequestAsync(order, request, ct);
            await orders.UpdateAsync(order, ct);
            return (await FindAsync(id, ct)).ToDto();
        }, cancellationToken);
    }

    public async Task<OrderDto> ChangeStatusAsync(Guid id, ChangeOrderStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireAdminAsync(ct);
            var order = await FindAsync(id, ct);
            if (request.Status == order.Status) return order.ToDto();
            if (request.Status == OrderStatus.Cancelled)
            {
                await CancelOrderAsync(order, ct);
            }
            else
            {
                var allowed = (order.Status, request.Status) is
                    (OrderStatus.Pending, OrderStatus.Confirmed) or
                    (OrderStatus.Confirmed, OrderStatus.Preparing) or
                    (OrderStatus.Preparing, OrderStatus.Ready) or
                    (OrderStatus.Ready, OrderStatus.Completed);
                if (!allowed) throw new BusinessRuleException("El cambio de estado no está permitido.");
                if (request.Status == OrderStatus.Completed &&
                    !order.Payments.Any(x => x.Status == PaymentStatus.Approved && x.Amount == order.Total && x.Currency == "COP"))
                    throw new BusinessRuleException("El pedido debe estar pagado antes de completarlo.");
                order.Status = request.Status;
                await orders.UpdateAsync(order, ct);
            }
            return (await FindAsync(id, ct)).ToDto();
        }, cancellationToken);
    }

    public async Task<OrderDto> CancelAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireUserAsync(ct);
            var order = await FindAsync(id, ct);
            var actor = await access.RequireOwnerOrAdminAsync(order.UserId, ct);
            if (order.Status == OrderStatus.Cancelled) return order.ToDto();
            if (actor.Role != UserRole.Admin && order.Status is not (OrderStatus.Pending or OrderStatus.Confirmed))
                throw new BusinessRuleException("El pedido ya está en preparación; contacta al local.");
            await CancelOrderAsync(order, ct);
            return (await FindAsync(id, ct)).ToDto();
        }, cancellationToken);
    }

    private async Task CancelOrderAsync(Order order, CancellationToken cancellationToken)
    {
        if (order.Status == OrderStatus.Completed) throw new BusinessRuleException("Un pedido completado no se puede cancelar.");
        if (order.Payments.Any(x => x.Status == PaymentStatus.Approved))
            throw new BusinessRuleException("Se debe resolver el reembolso antes de cancelar un pedido pagado.");
        if (order.Payments.Any(x => x.Status == PaymentStatus.Pending && x.Provider is not null))
            throw new BusinessRuleException("El pago debe cancelarse con la pasarela antes de cancelar el pedido.");

        foreach (var payment in order.Payments.Where(x => x.Status == PaymentStatus.Pending))
        {
            payment.Status = PaymentStatus.Cancelled;
            payment.FailureReason = "Pedido cancelado.";
            await payments.UpdateAsync(payment, cancellationToken);
        }
        order.Status = OrderStatus.Cancelled;
        await orders.UpdateAsync(order, cancellationToken);
    }

    private async Task ApplyRequestAsync(Order order, OrderRequestDto request, CancellationToken cancellationToken)
    {
        var items = new List<OrderItem>();
        foreach (var requested in request.Items)
        {
            var frappe = await frappes.GetByIdAsync(requested.FrappeId, cancellationToken)
                ?? throw new NotFoundException("Uno de los frappés no existe.");
            if (!frappe.IsActive || !frappe.IsAvailable)
                throw new BusinessRuleException($"El frappé '{frappe.Name}' no está disponible.");
            if (frappe.Price <= 0 || frappe.Price > ValidationRules.MaximumAmount)
                throw new BusinessRuleException("El catálogo contiene un precio inválido.");
            var existing = order.Items.SingleOrDefault(x => x.FrappeId == frappe.Id);
            items.Add(new OrderItem
            {
                Id = existing?.Id ?? Guid.NewGuid(), OrderId = order.Id, FrappeId = frappe.Id,
                FrappeName = frappe.Name, Flavor = frappe.Flavor, Toppings = frappe.Toppings,
                Quantity = requested.Quantity, UnitPrice = frappe.Price, Total = frappe.Price * requested.Quantity,
                Notes = ServiceGuard.OptionalText(requested.Notes)
            });
        }
        var subtotal = items.Sum(x => x.Total);
        var deliveryFee = request.DeliveryType == DeliveryType.Delivery ? options.Value.DeliveryFee : 0;
        if (subtotal + deliveryFee > ValidationRules.MaximumAmount)
            throw new BusinessRuleException("El total del pedido supera el monto permitido.");

        order.CustomerName = request.CustomerName.Trim();
        order.CustomerPhone = request.CustomerPhone.Trim();
        order.DeliveryType = request.DeliveryType;
        order.DeliveryAddress = request.DeliveryType == DeliveryType.Delivery ? request.DeliveryAddress!.Trim() : null;
        order.Notes = ServiceGuard.OptionalText(request.Notes);
        order.Items = items;
        order.Subtotal = subtotal;
        order.DeliveryFee = deliveryFee;
        order.Total = subtotal + deliveryFee;
    }

    private async Task<Order> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await orders.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Pedido no encontrado.");
}
