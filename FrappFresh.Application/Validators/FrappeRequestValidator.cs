using FluentValidation;
using FrappFresh.Application.DTOs.Frappes;

namespace FrappFresh.Application.Validators;

public class FrappeRequestValidator : AbstractValidator<FrappeRequestDto>
{
    public FrappeRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.Flavor).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Base).IsInEnum();
        RuleFor(x => x.Toppings).MaximumLength(500);
        RuleFor(x => x.Price).GreaterThan(0).LessThanOrEqualTo(ValidationRules.MaximumAmount)
            .PrecisionScale(12, 2, false);
        RuleFor(x => x.ImageUrl).MaximumLength(2048).Must(ValidationRules.IsHttpUrl)
            .WithMessage("La imagen debe ser una URL HTTP o HTTPS válida.");
    }
}
