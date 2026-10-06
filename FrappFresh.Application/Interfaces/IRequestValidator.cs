namespace FrappFresh.Application.Interfaces;

public interface IRequestValidator
{
    Task ValidateAsync<T>(T request, CancellationToken cancellationToken = default) where T : class;
}
