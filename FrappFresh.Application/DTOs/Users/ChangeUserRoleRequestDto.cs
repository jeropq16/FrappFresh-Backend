using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.DTOs.Users;

public class ChangeUserRoleRequestDto
{
    public UserRole Role { get; set; }
}
