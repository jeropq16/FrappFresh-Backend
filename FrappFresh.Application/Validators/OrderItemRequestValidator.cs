using FluentValidation;
using FrappFresh.Application.DTOs.Orders;

namespace FrappFresh.Application.Validators;

public class OrderItemRequestValidator : AbstractValidator<OrderItemRequestDto>
{
    public OrderItemRequestValidator()
    {
        RuleFor(x => x.FrappeId).NotEmpty();
        RuleFor(x => x.Quantity).InclusiveBetween(1, 99);
        RuleFor(x => x.Notes).MaximumLength(1000);
    }
}
