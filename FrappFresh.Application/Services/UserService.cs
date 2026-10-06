using FrappFresh.Application.DTOs.Users;
using FrappFresh.Application.Exceptions;
using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Entities;
using FrappFresh.Domain.Enums;
using FrappFresh.Domain.Interfaces;

namespace FrappFresh.Application.Services;

public class UserService(IUserRepository users, IPasswordService passwords, IRefreshTokenService sessions,
    IUserAccessService access, IRequestValidator validator, IUnitOfWork unitOfWork) : IUserService
{
    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await access.RequireAdminAsync(cancellationToken);
        return (await users.GetAllAsync(cancellationToken)).Select(x => x.ToDto()).ToArray();
    }

    public async Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await access.RequireOwnerOrAdminAsync(id, cancellationToken);
        return (await FindAsync(id, cancellationToken)).ToDto();
    }

    public async Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default) =>
        (await access.RequireUserAsync(cancellationToken)).ToDto();

    public async Task<UserDto> CreateAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireAdminAsync(ct);
            var email = request.Email.Trim().ToLowerInvariant();
            if (await users.GetByEmailAsync(email, ct) is not null) throw new ConflictException("El correo ya está registrado.");
            var user = new User
            {
                FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(), Email = email,
                PhoneNumber = request.PhoneNumber.Trim(), Address = ServiceGuard.OptionalText(request.Address), Role = request.Role
            };
            user.PasswordHash = passwords.HashPassword(user, request.Password);
            await users.AddAsync(user, ct);
            return user.ToDto();
        }, cancellationToken);
    }

    public async Task<UserDto> UpdateAsync(Guid id, UpdateUserRequestDto request, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            await access.RequireOwnerOrAdminAsync(id, ct);
            var user = await FindAsync(id, ct);
            var email = request.Email.Trim().ToLowerInvariant();
            var existing = await users.GetByEmailAsync(email, ct);
            if (existing is not null && existing.Id != id) throw new ConflictException("El correo ya está registrado.");
            var changedEmail = user.Email != email;
            user.FirstName = request.FirstName.Trim();
            user.LastName = request.LastName.Trim();
            user.Email = email;
            user.PhoneNumber = request.PhoneNumber.Trim();
            user.Address = ServiceGuard.OptionalText(request.Address);
            await users.UpdateAsync(user, ct);
            if (changedEmail) await sessions.RevokeAllAsync(user.Id, ct);
            return (await FindAsync(id, ct)).ToDto();
        }, cancellationToken);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAsync(request, cancellationToken);
        await unitOfWork.ExecuteAsync(async ct =>
        {
            var user = await access.RequireUserAsync(ct);
            if (!passwords.VerifyPassword(user, request.CurrentPassword))
                throw new UnauthorizedException("La contraseña actual es incorrecta.");
            user.PasswordHash = passwords.HashPassword(user, request.NewPassword);
            await users.UpdateAsync(user, ct);
            await sessions.RevokeAllAsync(user.Id, ct);
            return true;
        }, cancellationToken);
    }

    public async Task<UserDto> ChangeRoleAsync(Guid id, ChangeUserRoleRequestDto request, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            var actor = await access.RequireAdminAsync(ct);
            if (actor.Id == id && request.Role != UserRole.Admin)
                throw new BusinessRuleException("No puedes quitarte tu propio rol de administrador.");
            var user = await FindAsync(id, ct);
            if (user.Role != request.Role)
            {
                user.Role = request.Role;
                await users.UpdateAsync(user, ct);
                await sessions.RevokeAllAsync(user.Id, ct);
            }
            return (await FindAsync(id, ct)).ToDto();
        }, cancellationToken);
    }

    public async Task<UserDto> ChangeStatusAsync(Guid id, ChangeUserStatusRequestDto request, CancellationToken cancellationToken = default)
    {
        ServiceGuard.Id(id);
        await validator.ValidateAsync(request, cancellationToken);
        return await unitOfWork.ExecuteAsync(async ct =>
        {
            var actor = await access.RequireAdminAsync(ct);
            if (actor.Id == id && !request.IsActive)
                throw new BusinessRuleException("No puedes desactivar tu propia cuenta de administrador.");
            var user = await FindAsync(id, ct);
            user.IsActive = request.IsActive;
            await users.UpdateAsync(user, ct);
            if (!request.IsActive) await sessions.RevokeAllAsync(user.Id, ct);
            return (await FindAsync(id, ct)).ToDto();
        }, cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default) =>
        await ChangeStatusAsync(id, new ChangeUserStatusRequestDto { IsActive = false }, cancellationToken);

    private async Task<User> FindAsync(Guid id, CancellationToken cancellationToken) =>
        await users.GetByIdAsync(id, cancellationToken) ?? throw new NotFoundException("Usuario no encontrado.");
}
