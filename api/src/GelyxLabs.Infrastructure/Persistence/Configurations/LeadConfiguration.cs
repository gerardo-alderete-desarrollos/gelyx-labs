using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GelyxLabs.Domain.Entities;

namespace GelyxLabs.Infrastructure.Persistence.Configurations;

public class LeadConfiguration : IEntityTypeConfiguration<Lead>
{
    public void Configure(EntityTypeBuilder<Lead> builder)
    {
        builder.ToTable("leads");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Email)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(l => l.Phone)
            .HasMaxLength(50);

        builder.Property(l => l.Company)
            .HasMaxLength(200);

        builder.Property(l => l.Service)
            .HasMaxLength(100);

        builder.Property(l => l.Budget)
            .HasMaxLength(100);

        builder.Property(l => l.Message)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(l => l.Status)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(l => l.Notes)
            .HasMaxLength(4000);

        builder.Property(l => l.ResponseChannel)
            .HasMaxLength(50);

        builder.Property(l => l.CreatedAt)
            .IsRequired();

        builder.HasIndex(l => l.Email);
        builder.HasIndex(l => l.Status);
    }
}