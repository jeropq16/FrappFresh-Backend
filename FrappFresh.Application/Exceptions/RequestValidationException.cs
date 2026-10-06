namespace FrappFresh.Application.Exceptions;

public class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("Revisa los datos enviados.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
