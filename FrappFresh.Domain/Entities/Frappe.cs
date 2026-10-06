using FrappFresh.Domain.Enums;

namespace FrappFresh.Domain.Entities;

public class Frappe : Entity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Flavor { get; set; } = string.Empty;
    public BeverageBase Base { get; set; } = BeverageBase.Water;
    public string? Toppings { get; set; }
    public decimal Price { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsPremium { get; set; }
    public bool IsAvailable { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
