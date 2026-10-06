namespace FrappFresh.Domain.Entities;

public class OrderItem : Entity
{
    public Guid OrderId { get; set; }
    public Guid FrappeId { get; set; }

    // Copia de los datos vendidos para conservar el historial si cambia el catálogo.
    public string FrappeName { get; set; } = string.Empty;
    public string Flavor { get; set; } = string.Empty;
    public string? Toppings { get; set; }
    public int Quantity { get; set; } = 1;
    public decimal UnitPrice { get; set; }
    public decimal Total { get; set; }
    public string? Notes { get; set; }

    public Order Order { get; set; } = null!;
    public Frappe Frappe { get; set; } = null!;
}
