using FrappFresh.Application.DTOs.Auth;
using FrappFresh.Application.Security;

namespace FrappFresh.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default);
    Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default);
    Task<TokenPair> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task LogoutAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default);
}
