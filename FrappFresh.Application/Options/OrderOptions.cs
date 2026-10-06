namespace FrappFresh.Application.Options;

public class OrderOptions
{
    public const string SectionName = "Orders";
    public decimal DeliveryFee { get; set; } = 3000m;
}
