using FrappFresh.Domain.Entities;

namespace FrappFresh.Domain.Interfaces;

public interface IFrappeRepository : IRepository<Frappe>
{
    Task<IReadOnlyList<Frappe>> GetAvailableAsync(CancellationToken cancellationToken = default);
}
