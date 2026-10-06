using FrappFresh.Application.DTOs.Users;
using FrappFresh.Application.Security;

namespace FrappFresh.Application.DTOs.Auth;

public record AuthResponseDto(UserDto User, TokenPair Tokens);
