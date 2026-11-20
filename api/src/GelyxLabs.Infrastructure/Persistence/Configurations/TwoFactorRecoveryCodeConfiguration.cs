using GelyxLabs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GelyxLabs.Infrastructure.Persistence.Configurations;

public sealed class TwoFactorRecoveryCodeConfiguration : IEntityTypeConfiguration<TwoFactorRecoveryCode>
{
    public void Configure(EntityTypeBuilder<TwoFactorRecoveryCode> builder)
    {
        builder.ToTable("two_factor_recovery_codes");
        builder.HasKey(code => code.Id);
        builder.Property(code => code.CodeHash).HasMaxLength(512).IsRequired();
        builder.HasIndex(code => new { code.UserId, code.UsedAt });
        builder.HasOne(code => code.User).WithMany().HasForeignKey(code => code.UserId).OnDelete(DeleteBehavior.Cascade);
    }
}
