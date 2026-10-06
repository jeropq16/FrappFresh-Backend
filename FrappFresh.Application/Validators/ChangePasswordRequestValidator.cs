using FluentValidation;
using FrappFresh.Application.DTOs.Users;

namespace FrappFresh.Application.Validators;

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequestDto>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(128);
        RuleFor(x => x.NewPassword).Password().NotEqual(x => x.CurrentPassword)
            .WithMessage("La nueva contraseña debe ser diferente.");
        RuleFor(x => x.ConfirmPassword).NotEmpty().Equal(x => x.NewPassword)
            .WithMessage("Las contraseñas no coinciden.");
    }
}
