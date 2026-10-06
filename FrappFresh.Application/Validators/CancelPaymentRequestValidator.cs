using FluentValidation;
using FrappFresh.Application.DTOs.Payments;

namespace FrappFresh.Application.Validators;

public class CancelPaymentRequestValidator : AbstractValidator<CancelPaymentRequestDto>
{
    public CancelPaymentRequestValidator() => RuleFor(x => x.Reason).NotEmpty().MaximumLength(500);
}
