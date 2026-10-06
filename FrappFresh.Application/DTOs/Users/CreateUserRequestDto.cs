using FrappFresh.Application.DTOs.Auth;
using FrappFresh.Domain.Enums;

namespace FrappFresh.Application.DTOs.Users;

// Creación desde administración. El registro público usa RegisterRequestDto sin rol.
public class CreateUserRequestDto : RegisterRequestDto
{
    public UserRole Role { get; set; } = UserRole.Customer;
}
