using FluentValidation;
using FrappFresh.Application.DTOs.Users;

namespace FrappFresh.Application.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequestDto>
{
    public CreateUserRequestValidator()
    {
        Include(new RegisterRequestValidator());
        RuleFor(x => x.Role).IsInEnum();
    }
}
