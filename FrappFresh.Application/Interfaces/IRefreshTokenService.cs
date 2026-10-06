using FrappFresh.Application.Security;

namespace FrappFresh.Application.Interfaces;

public interface IRefreshTokenService
{
    // Invocar CreateAsync después de validar las credenciales del usuario.
    Task<TokenPair> CreateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<TokenPair?> RotateAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAllAsync(Guid userId, CancellationToken cancellationToken = default);
}
