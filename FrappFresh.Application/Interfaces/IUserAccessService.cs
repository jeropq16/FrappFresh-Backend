using FrappFresh.Domain.Entities;

namespace FrappFresh.Application.Interfaces;

public interface IUserAccessService
{
    Task<User> RequireUserAsync(CancellationToken cancellationToken = default);
    Task<User> RequireAdminAsync(CancellationToken cancellationToken = default);
    Task<User> RequireOwnerOrAdminAsync(Guid ownerId, CancellationToken cancellationToken = default);
}
