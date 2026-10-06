using FluentValidation;
using FrappFresh.Application.DTOs.Orders;
using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.Validators;

public class OrderRequestValidator : AbstractValidator<OrderRequestDto>
{
    public OrderRequestValidator()
    {
        RuleFor(x => x.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.CustomerPhone).Phone();
        RuleFor(x => x.DeliveryType).IsInEnum();
        RuleFor(x => x.DeliveryAddress).MaximumLength(300);
        RuleFor(x => x.DeliveryAddress).NotEmpty().When(x => x.DeliveryType == DeliveryType.Delivery);
        RuleFor(x => x.Notes).MaximumLength(1000);
        RuleFor(x => x.Items).NotEmpty().Must(items => items is null || items.Count <= 20)
            .WithMessage("El pedido admite hasta 20 productos diferentes.")
            .Must(items => items is null || items.Where(x => x is not null).Select(x => x.FrappeId).Distinct().Count() ==
                items.Count(x => x is not null)).WithMessage("No repitas un frappé; ajusta su cantidad.");
        RuleForEach(x => x.Items).NotNull().SetValidator(new OrderItemRequestValidator());
    }
}
