namespace FrappFresh.Infrastructure.Options;

public class SeedOptions
{
    public const string SectionName = "Seed";

    public bool Enabled { get; set; }
    public string AdminEmail { get; set; } = "admin@frappfresh.test";
    public string AdminPassword { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = "cliente@frappfresh.test";
    public string CustomerPassword { get; set; } = string.Empty;
}
