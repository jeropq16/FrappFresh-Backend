using FrappFresh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrappFresh.Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : EntityConfiguration<OrderItem>
{
    public override void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        base.Configure(builder);
        builder.ToTable("OrderItems", table => table.HasCheckConstraint("CK_OrderItems_Amounts",
            "`Quantity` > 0 AND `UnitPrice` >= 0 AND `Total` = `Quantity` * `UnitPrice`"));
        builder.Property(x => x.FrappeName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Flavor).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Toppings).HasMaxLength(500);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.UnitPrice).HasPrecision(12, 2);
        builder.Property(x => x.Total).HasPrecision(12, 2);
        builder.HasOne(x => x.Order).WithMany(x => x.Items)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(x => x.Frappe).WithMany()
            .HasForeignKey(x => x.FrappeId).OnDelete(DeleteBehavior.Restrict);
    }
}
