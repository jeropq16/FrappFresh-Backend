using System.Data;
using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;

namespace FrappFresh.Infrastructure.Persistence;

public class UnitOfWork(AppDbContext context) : IUnitOfWork
{
    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)
    {
        // Los repositorios comparten el mismo contexto scoped. Serializable protege
        // las comprobaciones de pedido/pago incluso con solicitudes simultáneas.
        await using var transaction = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        try
        {
            var result = await action(cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return result;
        }
        catch (Exception error)
        {
            await transaction.RollbackAsync(CancellationToken.None);
            context.ChangeTracker.Clear();
            var databaseError = error as MySqlException ?? error.InnerException as MySqlException;
            if (error is DbUpdateConcurrencyException || databaseError?.Number is 1062 or 1205 or 1213)
                throw new ConflictException("Los datos cambiaron o ya existe el registro. Actualiza e intenta nuevamente.", error);
            throw;
        }
    }
}
