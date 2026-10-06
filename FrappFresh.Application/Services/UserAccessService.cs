using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Enums;
using FrappFresh.Domain.Interfaces;

namespace FrappFresh.Application.Services;

public class UserAccessService(ICurrentUser currentUser, IUserRepository users) : IUserAccessService
{
    public async Task<User> RequireUserAsync(CancellationToken cancellationToken = default)
    {
        if (currentUser.UserId is not Guid id || id == Guid.Empty) throw new UnauthorizedException();
        var user = await users.GetByIdAsync(id, cancellationToken);
        if (user is null || !user.IsActive) throw new UnauthorizedException("La sesión no está disponible.");
        return user;
    }

    public async Task<User> RequireAdminAsync(CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (user.Role != UserRole.Admin) throw new ForbiddenException();
        return user;
    }

    public async Task<User> RequireOwnerOrAdminAsync(Guid ownerId, CancellationToken cancellationToken = default)
    {
        var user = await RequireUserAsync(cancellationToken);
        if (user.Id != ownerId && user.Role != UserRole.Admin) throw new ForbiddenException();
        return user;
    }
}
