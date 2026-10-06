using FluentValidation;
using FrappFresh.Application.DTOs.Users;

namespace FrappFresh.Application.Validators;

public class ChangeUserRoleRequestValidator : AbstractValidator<ChangeUserRoleRequestDto>
{
    public ChangeUserRoleRequestValidator() => RuleFor(x => x.Role).IsInEnum();
}
