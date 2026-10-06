using FrappFresh.Application.Interfaces;
using FrappFresh.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace FrappFresh.Infrastructure.Security;

public class PasswordService : IPasswordService
{
    private readonly PasswordHasher<User> _hasher = new();

    public string HashPassword(User user, string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);
        return _hasher.HashPassword(user, password);
    }

    public bool VerifyPassword(User user, string password)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrEmpty(user.PasswordHash)) return false;
        try
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = _hasher.HashPassword(user, password); // El caso de uso guarda el hash actualizado.
            return result != PasswordVerificationResult.Failed;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
