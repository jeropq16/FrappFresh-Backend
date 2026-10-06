using FrappFresh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrappFresh.Infrastructure.Persistence.Configurations;

public class PaymentConfiguration : EntityConfiguration<Payment>
{
    public override void Configure(EntityTypeBuilder<Payment> builder)
    {
        base.Configure(builder);
        builder.ToTable("Payments", table => table.HasCheckConstraint("CK_Payments_Amount", "`Amount` >= 0"));
        builder.Property(x => x.Amount).HasPrecision(12, 2);
        builder.Property(x => x.Currency).HasMaxLength(3).IsRequired();
        builder.Property(x => x.Method).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Provider).HasMaxLength(60);
        builder.Property(x => x.ProviderTransactionId).HasMaxLength(150).UseCollation("utf8mb4_bin");
        builder.Property(x => x.PaymentReference).HasMaxLength(150);
        builder.Property(x => x.FailureReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.Provider, x.ProviderTransactionId }).IsUnique();
        builder.HasOne(x => x.Order).WithMany(x => x.Payments)
            .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
    }
}
