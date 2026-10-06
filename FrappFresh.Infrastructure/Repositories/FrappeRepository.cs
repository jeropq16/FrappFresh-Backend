using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Interfaces;
using FrappFresh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FrappFresh.Infrastructure.Repositories;

public class FrappeRepository(AppDbContext context) : Repository<Frappe>(context), IFrappeRepository
{
    public async Task<IReadOnlyList<Frappe>> GetAvailableAsync(CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().Where(x => x.IsActive && x.IsAvailable)
            .OrderBy(x => x.Name).ThenBy(x => x.Id).ToListAsync(cancellationToken);
}
