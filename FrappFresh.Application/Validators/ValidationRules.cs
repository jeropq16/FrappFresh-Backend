using FluentValidation;

namespace FrappFresh.Application.Validators;

internal static class ValidationRules
{
    public const decimal MaximumAmount = 9999999999.99m;

    public static IRuleBuilderOptions<T, string> Password<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().Length(12, 128).Matches("[A-Z]").WithMessage("Debe incluir una mayúscula.")
            .Matches("[a-z]").WithMessage("Debe incluir una minúscula.")
            .Matches("[0-9]").WithMessage("Debe incluir un número.");

    public static IRuleBuilderOptions<T, string> Phone<T>(this IRuleBuilder<T, string> rule) =>
        rule.NotEmpty().Matches(@"^\+?[1-9][0-9]{7,14}$")
            .WithMessage("Usa entre 8 y 15 dígitos con indicativo de país.");

    public static bool IsHttpUrl(string? value) =>
        string.IsNullOrWhiteSpace(value) || (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
        (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp) && string.IsNullOrEmpty(uri.UserInfo));
}
