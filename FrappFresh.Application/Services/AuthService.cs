using FrappFresh.Application.DTOs.Auth;
using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using FrappFresh.Application.Security;
using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Enums;
using FrappFresh.Domain.Interfaces;

namespace FrappFresh.Application.Services;

public class AuthService(IUserRepository users, IPasswordService passwords, IRefreshTokenService sessions,
    IRequestValidator validator, IUnitOfWork unitOfWork) : IAuthService
{
    public async Task<AuthResponseDto> RegisterAsync(RegisterRequestDto request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            var email = request.Email.Trim().ToLowerInvariant();
            if (await users.GetByEmailAsync(email, ct) is not null)
                throw new ConflictException("El correo ya está registrado.");

            var user = new User
            {
                FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
                Email = email, PhoneNumber = request.PhoneNumber.Trim(),
                Address = ServiceGuard.OptionalText(request.Address), Role = UserRole.Customer, IsActive = true
            };
            user.PasswordHash = passwords.HashPassword(user, request.Password);
            await users.AddAsync(user, ct);
            var tokens = await sessions.CreateAsync(user.Id, ct);
            return new AuthResponseDto(user.ToDto(), tokens);
        }, cancellationToken);
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequestDto request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            var user = await users.GetByEmailAsync(request.Email, ct);
            if (user is null || !user.IsActive) throw new UnauthorizedException("Correo o contraseña incorrectos.");
            var oldHash = user.PasswordHash;
            if (!passwords.VerifyPassword(user, request.Password))
                throw new UnauthorizedException("Correo o contraseña incorrectos.");
            if (oldHash != user.PasswordHash) await users.UpdateAsync(user, ct);
            var tokens = await sessions.CreateAsync(user.Id, ct);
            return new AuthResponseDto(user.ToDto(), tokens);
        }, cancellationToken);
    }

    public async Task<TokenPair> RefreshAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(request, cancellationToken);
        return await sessions.RotateAsync(request.RefreshToken, cancellationToken)
            ?? throw new UnauthorizedException("La sesión venció o el token ya fue utilizado.");
    }

    public async Task LogoutAsync(RefreshTokenRequestDto request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(request, cancellationToken);
        await sessions.RevokeAsync(request.RefreshToken, cancellationToken);
    }
}
