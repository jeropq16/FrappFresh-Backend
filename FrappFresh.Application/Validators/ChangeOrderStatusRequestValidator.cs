using FluentValidation;
using FrappFresh.Application.DTOs.Orders;

namespace FrappFresh.Application.Validators;

public class ChangeOrderStatusRequestValidator : AbstractValidator<ChangeOrderStatusRequestDto>
{
    public ChangeOrderStatusRequestValidator() => RuleFor(x => x.Status).IsInEnum();
}
