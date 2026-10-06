using FluentValidation;
using FrappFresh.Application.DTOs.Payments;

namespace FrappFresh.Application.Validators;

public class CreatePaymentRequestValidator : AbstractValidator<CreatePaymentRequestDto>
{
    public CreatePaymentRequestValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty();
        RuleFor(x => x.Method).IsInEnum();
    }
}
