using FrappFresh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrappFresh.Infrastructure.Persistence.Configurations;

public class FrappeConfiguration : EntityConfiguration<Frappe>
{
    public override void Configure(EntityTypeBuilder<Frappe> builder)
    {
        base.Configure(builder);
        builder.ToTable("Frappes", table => table.HasCheckConstraint("CK_Frappes_Price", "`Price` >= 0"));
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(2000).IsRequired();
        builder.Property(x => x.Flavor).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Toppings).HasMaxLength(500);
        builder.Property(x => x.ImageUrl).HasMaxLength(2048);
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.Property(x => x.Base).HasConversion<int>();
        builder.HasIndex(x => new { x.IsActive, x.IsAvailable });
    }
}
