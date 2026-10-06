namespace FrappFresh.Domain.Entities;

public class RefreshToken : Entity
{
    public Guid UserId { get; set; }

    // Guardar el hash del refresh token, nunca el token original.
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }

    public User User { get; set; } = null!;
}
