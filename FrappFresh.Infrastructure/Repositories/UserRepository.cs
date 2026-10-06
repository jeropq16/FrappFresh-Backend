using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Interfaces;
using FrappFresh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FrappFresh.Infrastructure.Repositories;

public class UserRepository(AppDbContext context) : Repository<User>(context), IUserRepository
{
    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        return Set.AsNoTracking().SingleOrDefaultAsync(x => x.Email == normalizedEmail, cancellationToken);
    }
}
