using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Interfaces;
using FrappFresh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FrappFresh.Infrastructure.Repositories;

public class Repository<T>(AppDbContext context) : IRepository<T> where T : Entity
{
    protected AppDbContext Context { get; } = context;
    protected DbSet<T> Set => Context.Set<T>();
    protected virtual IQueryable<T> Query => Set;

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await Query.AsNoTracking().OrderBy(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);

    public virtual Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Query.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken);

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        await Set.AddAsync(entity, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken cancellationToken = default)
    {
        var existing = await Set.FindAsync([entity.Id], cancellationToken)
            ?? throw new KeyNotFoundException($"{typeof(T).Name} no encontrado.");
        Context.Entry(existing).CurrentValues.SetValues(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }

    public virtual async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await Set.FindAsync([id], cancellationToken)
            ?? throw new KeyNotFoundException($"{typeof(T).Name} no encontrado.");
        Set.Remove(entity);
        await Context.SaveChangesAsync(cancellationToken);
    }
}
