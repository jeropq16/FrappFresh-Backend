using FluentValidation;
using FrappFresh.Application.DTOs.Users;

namespace FrappFresh.Application.Validators;

public class UpdateUserRequestValidator : AbstractValidator<UpdateUserRequestDto>
{
    public UpdateUserRequestValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Email).NotEmpty().MaximumLength(254).EmailAddress();
        RuleFor(x => x.PhoneNumber).Phone();
        RuleFor(x => x.Address).MaximumLength(300);
    }
}
