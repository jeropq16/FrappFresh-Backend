using FrappFresh.Application.Exceptions;

namespace FrappFresh.Application.Services;

internal static class ServiceGuard
{
    public static void Id(Guid id)
    {
        if (id == Guid.Empty)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["Id"] = ["El identificador es obligatorio."] });
    }

    public static string? OptionalText(string? text) => string.IsNullOrWhiteSpace(text) ? null : text.Trim();
}
