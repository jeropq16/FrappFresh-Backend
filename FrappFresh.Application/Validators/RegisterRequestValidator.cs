using FluentValidation;
using FrappFresh.Application.DTOs.Auth;

namespace FrappFresh.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequestDto>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(x => x.PhoneNumber).Phone();
        RuleFor(x => x.Address).MaximumLength(300);
        RuleFor(x => x.Password).Password();
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.Password).WithMessage("Las contraseñas no coinciden.");
    }
}
