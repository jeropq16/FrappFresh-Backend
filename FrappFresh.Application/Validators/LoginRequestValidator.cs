using FluentValidation;
using FrappFresh.Application.DTOs.Auth;

namespace FrappFresh.Application.Validators;

public class LoginRequestValidator : AbstractValidator<LoginRequestDto>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        // No aplicar la política de contraseñas nuevas al login de cuentas existentes.
        RuleFor(x => x.Password).NotEmpty().MaximumLength(128);
    }
}
