using FluentValidation;
using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace FrappFresh.Application.Validators;

public class RequestValidator(IServiceProvider services) : IRequestValidator
{
    public async Task ValidateAsync<T>(T request, CancellationToken cancellationToken = default) where T : class
    {
        if (request is null)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["Request"] = ["La solicitud es obligatoria."] });

        var validator = services.GetRequiredService<IValidator<T>>();
        var result = await validator.ValidateAsync(request, cancellationToken);
        if (!result.IsValid)
            throw new RequestValidationException(result.Errors.GroupBy(x => x.PropertyName)
                .ToDictionary(x => x.Key, x => x.Select(error => error.ErrorMessage).Distinct().ToArray()));
    }
}
