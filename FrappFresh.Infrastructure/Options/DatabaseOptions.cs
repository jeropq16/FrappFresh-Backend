using System.ComponentModel.DataAnnotations;

namespace FrappFresh.Infrastructure.Options;

public class DatabaseOptions
{
    public const string SectionName = "Database";

    [Required]
    public string ServerVersion { get; set; } = "8.0.36";
    [Range(1, 300)]
    public int CommandTimeoutSeconds { get; set; } = 30;
    public bool ApplyMigrationsOnStartup { get; set; }
}
