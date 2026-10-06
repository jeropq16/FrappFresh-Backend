using System.ComponentModel.DataAnnotations;

namespace FrappFresh.Infrastructure.Options;

public class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    public string Issuer { get; set; } = string.Empty;
    [Required]
    public string Audience { get; set; } = string.Empty;
    [Required, MinLength(32)]
    public string SigningKey { get; set; } = string.Empty;
    [Range(1, 60)]
    public int AccessTokenMinutes { get; set; } = 15;
    [Range(1, 90)]
    public int RefreshTokenDays { get; set; } = 7;
}
