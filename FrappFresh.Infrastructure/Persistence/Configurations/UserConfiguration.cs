using FrappFresh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrappFresh.Infrastructure.Persistence.Configurations;

public class UserConfiguration : EntityConfiguration<User>
{
    public override void Configure(EntityTypeBuilder<User> builder)
    {
        base.Configure(builder);
        builder.ToTable("Users");
        builder.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.LastName).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(254).IsRequired();
        builder.HasIndex(x => x.Email).IsUnique();
        builder.Property(x => x.PhoneNumber).HasMaxLength(20).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Role).HasConversion<int>();
    }
}
