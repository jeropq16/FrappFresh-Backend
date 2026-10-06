using FrappFresh.Application.DTOs.Users;

namespace FrappFresh.Application.Interfaces;

public interface IUserService
{
    Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<UserDto> GetMeAsync(CancellationToken cancellationToken = default);
    Task<UserDto> CreateAsync(CreateUserRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> UpdateAsync(Guid id, UpdateUserRequestDto request, CancellationToken cancellationToken = default);
    Task ChangePasswordAsync(ChangePasswordRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> ChangeRoleAsync(Guid id, ChangeUserRoleRequestDto request, CancellationToken cancellationToken = default);
    Task<UserDto> ChangeStatusAsync(Guid id, ChangeUserStatusRequestDto request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
