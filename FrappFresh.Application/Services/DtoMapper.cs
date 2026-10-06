using FrappFresh.Application.DTOs.Frappes;
using FrappFresh.Application.DTOs.Orders;
using FrappFresh.Application.DTOs.Payments;
using FrappFresh.Application.DTOs.Users;
using FrappFresh.Domain.Entities;

namespace FrappFresh.Application.Services;

internal static class DtoMapper
{
    public static UserDto ToDto(this User user) => new(user.Id, user.FirstName, user.LastName, user.Email,
        user.PhoneNumber, user.Address, user.Role, user.IsActive, user.CreatedAt, user.UpdatedAt);

    public static FrappeDto ToDto(this Frappe frappe) => new(frappe.Id, frappe.Name, frappe.Description, frappe.Flavor,
        frappe.Base, frappe.Toppings, frappe.Price, frappe.ImageUrl, frappe.IsPremium, frappe.IsAvailable,
        frappe.IsActive, frappe.CreatedAt, frappe.UpdatedAt);

    public static PaymentDto ToDto(this Payment payment) => new(payment.Id, payment.OrderId, payment.Amount,
        payment.Currency, payment.Method, payment.Status, payment.Provider, payment.ProviderTransactionId,
        payment.PaymentReference, payment.FailureReason, payment.PaidAt, payment.CreatedAt, payment.UpdatedAt);

    public static OrderItemDto ToDto(this OrderItem item) => new(item.Id, item.OrderId, item.FrappeId, item.FrappeName,
        item.Flavor, item.Toppings, item.Quantity, item.UnitPrice, item.Total, item.Notes, item.CreatedAt, item.UpdatedAt);

    public static OrderDto ToDto(this Order order) => new(order.Id, order.OrderNumber, order.UserId,
        order.CustomerName, order.CustomerPhone, order.DeliveryType, order.DeliveryAddress, order.Notes,
        order.Status, order.Subtotal, order.DeliveryFee, order.Total,
        order.Items.Select(ToDto).ToArray(), order.Payments.Select(ToDto).ToArray(), order.CreatedAt, order.UpdatedAt);
}
