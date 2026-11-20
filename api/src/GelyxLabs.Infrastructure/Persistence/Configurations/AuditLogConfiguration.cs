using GelyxLabs.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace GelyxLabs.Infrastructure.Persistence.Configurations;

public sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("audit_logs");
        builder.HasKey(log => log.Id);
        builder.Property(log => log.Action).HasMaxLength(200).IsRequired();
        builder.Property(log => log.EntityType).HasMaxLength(200).IsRequired();
        builder.Property(log => log.EntityId).HasMaxLength(200);
        builder.Property(log => log.Metadata).HasColumnType("jsonb").IsRequired();
        builder.Property(log => log.Ip).HasMaxLength(45);
        builder.Property(log => log.CreatedAt).IsRequired();
        builder.HasIndex(log => new { log.EntityType, log.EntityId });
        builder.HasIndex(log => log.ActorUserId);
        builder.HasIndex(log => log.CreatedAt);
    }
}
