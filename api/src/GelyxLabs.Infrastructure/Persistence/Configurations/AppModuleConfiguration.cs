using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using GelyxLabs.Domain.Entities;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System.Text.Json;

namespace GelyxLabs.Infrastructure.Persistence.Configurations;

public class AppModuleConfiguration : IEntityTypeConfiguration<AppModule>
{
    public void Configure(EntityTypeBuilder<AppModule> builder)
    {
        builder.ToTable("modules");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(m => m.Description)
            .HasMaxLength(500);

        var translationsConverter = new ValueConverter<Dictionary<string, ModuleTranslation>, string>(
            translations => JsonSerializer.Serialize(translations, (JsonSerializerOptions?)null),
            value => JsonSerializer.Deserialize<Dictionary<string, ModuleTranslation>>(value, (JsonSerializerOptions?)null) ??
                     new Dictionary<string, ModuleTranslation>());
        var translationsComparer = new ValueComparer<Dictionary<string, ModuleTranslation>>(
            (left, right) => JsonSerializer.Serialize(left, (JsonSerializerOptions?)null) == JsonSerializer.Serialize(right, (JsonSerializerOptions?)null),
            value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null).GetHashCode(StringComparison.Ordinal),
            value => JsonSerializer.Deserialize<Dictionary<string, ModuleTranslation>>(
                JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                (JsonSerializerOptions?)null) ?? new Dictionary<string, ModuleTranslation>());
        builder.Property(m => m.Translations)
            .HasConversion(translationsConverter)
            .Metadata.SetValueComparer(translationsComparer);
        builder.Property(m => m.Translations)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(m => m.Icon)
            .HasMaxLength(100);

        builder.Property(m => m.Route)
            .HasMaxLength(200);

        builder.Property(m => m.SortOrder)
            .IsRequired();

        builder.Property(m => m.IsSystem)
            .IsRequired();

        builder.Property(m => m.IsActive)
            .IsRequired();

        builder.HasIndex(m => m.Key).IsUnique();

        builder.HasMany(m => m.Permissions)
            .WithOne(p => p.Module)
            .HasForeignKey(p => p.ModuleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}