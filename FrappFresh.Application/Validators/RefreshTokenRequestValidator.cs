using FluentValidation;
using FrappFresh.Application.DTOs.Auth;

namespace FrappFresh.Application.Validators;

public class RefreshTokenRequestValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenRequestValidator() => RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(256);
}
