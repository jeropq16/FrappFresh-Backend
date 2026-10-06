namespace FrappFresh.Application.Interfaces;

// Agrupa operaciones de repositorios que deben confirmarse o revertirse juntas.
public interface IUnitOfWork
{
    Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default);
}
