using GelyxLabs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GelyxLabs.Infrastructure.Persistence.Configurations;

public sealed class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_settings");
        builder.HasKey(setting => setting.Key);
        builder.Property(setting => setting.Key).HasMaxLength(200);
        builder.Property(setting => setting.Value).HasColumnType("jsonb").IsRequired();
        builder.Property(setting => setting.Type).HasMaxLength(20).IsRequired();
        builder.Property(setting => setting.UpdatedAt).IsRequired();
        builder.HasIndex(setting => setting.UpdatedBy);
    }
}
