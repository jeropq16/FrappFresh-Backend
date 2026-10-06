using FrappFresh.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FrappFresh.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : EntityConfiguration<RefreshToken>
{
    public override void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        base.Configure(builder);
        builder.ToTable("RefreshTokens");
        builder.Property(x => x.TokenHash).HasMaxLength(64).IsUnicode(false).UseCollation("ascii_bin").IsRequired();
        builder.Property(x => x.ReplacedByTokenHash).HasMaxLength(64).IsUnicode(false).UseCollation("ascii_bin");
        builder.Property(x => x.RevokedAt).IsConcurrencyToken();
        builder.HasIndex(x => x.TokenHash).IsUnique();
        builder.HasIndex(x => new { x.UserId, x.ExpiresAt });
        builder.HasOne(x => x.User).WithMany(x => x.RefreshTokens)
            .HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
