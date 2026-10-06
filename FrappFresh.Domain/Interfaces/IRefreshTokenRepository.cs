using FrappFresh.Domain.Entities;

namespace FrappFresh.Domain.Interfaces;

public interface IRefreshTokenRepository : IRepository<RefreshToken>
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<RefreshToken>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
}
