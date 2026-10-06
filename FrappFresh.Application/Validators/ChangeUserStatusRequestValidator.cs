using FluentValidation;
using FrappFresh.Application.DTOs.Users;

namespace FrappFresh.Application.Validators;

// Ambos valores booleanos son válidos; los permisos se comprueban en UserService.
public class ChangeUserStatusRequestValidator : AbstractValidator<ChangeUserStatusRequestDto>;
