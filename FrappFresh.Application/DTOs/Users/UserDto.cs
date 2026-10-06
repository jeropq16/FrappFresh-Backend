using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.DTOs.Users;

public record UserDto(Guid Id, string FirstName, string LastName, string Email, string PhoneNumber,
    string? Address, UserRole Role, bool IsActive, DateTimeOffset CreatedAt, DateTimeOffset? UpdatedAt);
