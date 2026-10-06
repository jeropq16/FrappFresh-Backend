using FrappFresh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrappFresh.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : EntityConfiguration<Order>
{
    public override void Configure(EntityTypeBuilder<Order> builder)
    {
        base.Configure(builder);
        builder.ToTable("Orders", table => table.HasCheckConstraint("CK_Orders_Amounts",
            "`Subtotal` >= 0 AND `DeliveryFee` >= 0 AND `Total` = `Subtotal` + `DeliveryFee`"));
        builder.Property(x => x.OrderNumber).HasMaxLength(40).IsRequired();
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.Property(x => x.CustomerName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CustomerPhone).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DeliveryAddress).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.DeliveryType).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Subtotal).HasPrecision(12, 2);
        builder.Property(x => x.DeliveryFee).HasPrecision(12, 2);
        builder.Property(x => x.Total).HasPrecision(12, 2);
        builder.HasIndex(x => new { x.UserId, x.CreatedAt });
        builder.HasOne(x => x.User).WithMany(x => x.Orders)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}
