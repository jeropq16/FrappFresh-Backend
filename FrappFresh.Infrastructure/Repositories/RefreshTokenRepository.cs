using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Interfaces;
using FrappFresh.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FrappFresh.Infrastructure.Repositories;

public class RefreshTokenRepository(AppDbContext context) : Repository<RefreshToken>(context), IRefreshTokenRepository
{
    public Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        Set.AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

    public async Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        await Set.AsNoTracking().Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAt).ThenBy(x => x.Id).ToListAsync(cancellationToken);
}
