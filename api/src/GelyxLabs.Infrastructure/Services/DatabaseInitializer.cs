using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Application.Features.Settings;
using GelyxLabs.Infrastructure.Persistence;

namespace GelyxLabs.Infrastructure.Services;

public interface IDatabaseInitializer
{
    Task InitializeAsync(CancellationToken cancellationToken = default);
}

public class DatabaseInitializer : IDatabaseInitializer
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseInitializer> _logger;

    private const string AdminRoleName = "Admin";
    private const string SoporteRoleName = "Soporte";
    private const string UsuarioRoleName = "Usuario";

    public DatabaseInitializer(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<DatabaseInitializer> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var migrateOnStartup = _configuration.GetValue("MIGRATE_ON_STARTUP", false);
        if (migrateOnStartup)
        {
            _logger.LogInformation("Applying database migrations...");
            await context.Database.MigrateAsync(cancellationToken);
            _logger.LogInformation("Database migrations applied");
        }
        else
        {
            _logger.LogInformation("MIGRATE_ON_STARTUP is false, skipping migrations");
        }

        await SeedSettingsAsync(context, cancellationToken);
        await SeedRolesAsync(scope, cancellationToken);
        await SeedAdminAsync(scope, cancellationToken);
        scope.ServiceProvider.GetRequiredService<IPermissionService>().InvalidateAll();
    }

    private async Task SeedSettingsAsync(AppDbContext context, CancellationToken cancellationToken)
    {
        var initialSettings = new List<(string Key, string Value, string Type)>
        {
            ("app.name", System.Text.Json.JsonSerializer.Serialize(_configuration["App:Name"] ?? "GelyxLabs"), SettingsCatalog.StringType),
            ("auth.allowSelfRegistration", System.Text.Json.JsonSerializer.Serialize(_configuration.GetValue("Auth:AllowSelfRegistration", false)), SettingsCatalog.BooleanType),
            ("app.defaultLocale", System.Text.Json.JsonSerializer.Serialize(_configuration["App:DefaultLocale"] ?? "es"), SettingsCatalog.StringType)
        };

        foreach (var flag in _configuration.GetSection("FeatureFlags").GetChildren())
        {
            if (bool.TryParse(flag.Value, out var enabled) &&
                SettingsCatalog.TryGetType($"flags.{flag.Key}", out _))
            {
                initialSettings.Add(($"flags.{flag.Key}", System.Text.Json.JsonSerializer.Serialize(enabled), SettingsCatalog.BooleanType));
            }
        }

        var existingKeys = await context.AppSettings
            .Where(setting => initialSettings.Select(initial => initial.Key).Contains(setting.Key))
            .Select(setting => setting.Key)
            .ToArrayAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var (key, value, type) in initialSettings.Where(initial => !existingKeys.Contains(initial.Key)))
        {
            context.AppSettings.Add(new AppSetting { Key = key, Value = value, Type = type, UpdatedAt = now });
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedRolesAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        var rolesToSeed = new[]
        {
            new { Name = AdminRoleName, Description = "Administrador del sistema con acceso total", IsSystem = true },
            new { Name = SoporteRoleName, Description = "Rol de soporte técnico", IsSystem = true },
            new { Name = UsuarioRoleName, Description = "Usuario estándar del sistema", IsSystem = true }
        };

        foreach (var roleData in rolesToSeed)
        {
            var existingRole = await roleManager.FindByNameAsync(roleData.Name);
            if (existingRole is null)
            {
                var role = new AppRole
                {
                    Name = roleData.Name,
                    NormalizedName = roleData.Name.ToUpperInvariant(),
                    Description = roleData.Description,
                    IsSystem = roleData.IsSystem
                };
                var result = await roleManager.CreateAsync(role);
                if (result.Succeeded)
                {
                    _logger.LogInformation("Created system role: {RoleName}", roleData.Name);
                }
                else
                {
                    _logger.LogError("Failed to create role {RoleName}: {Errors}", roleData.Name, string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }
            else
            {
                var updated = false;
                if (existingRole.Description != roleData.Description)
                {
                    existingRole.Description = roleData.Description;
                    updated = true;
                }
                if (existingRole.IsSystem != roleData.IsSystem)
                {
                    existingRole.IsSystem = roleData.IsSystem;
                    updated = true;
                }
                if (updated)
                {
                    await roleManager.UpdateAsync(existingRole);
                    _logger.LogInformation("Updated system role: {RoleName}", roleData.Name);
                }
            }
        }
    }

    private async Task SeedAdminAsync(IServiceScope scope, CancellationToken cancellationToken)
    {
        var adminEmail = _configuration["SEED_ADMIN_EMAIL"];
        var adminPassword = _configuration["SEED_ADMIN_PASSWORD"];

        if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
        {
            _logger.LogInformation("SEED_ADMIN_EMAIL or SEED_ADMIN_PASSWORD not configured, skipping admin user creation");
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

        var existingUser = await userManager.FindByEmailAsync(adminEmail);
        if (existingUser is null)
        {
            var adminRole = await roleManager.FindByNameAsync(AdminRoleName);
            if (adminRole is null)
            {
                _logger.LogError("Admin role not found, cannot assign to seed admin user");
                return;
            }

            var adminUser = new AppUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FullName = "Administrador",
                IsActive = true,
                MustChangePassword = true
            };

            var createResult = await userManager.CreateAsync(adminUser, adminPassword);
            if (!createResult.Succeeded)
            {
                _logger.LogError("Failed to create admin user: {Errors}", string.Join(", ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            var addRoleResult = await userManager.AddToRoleAsync(adminUser, AdminRoleName);
            if (!addRoleResult.Succeeded)
            {
                _logger.LogError("Failed to assign Admin role to seed admin user: {Errors}", string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                return;
            }

            _logger.LogInformation("Created seed admin user: {Email}", adminEmail);
        }
        else
        {
            var isInAdminRole = await userManager.IsInRoleAsync(existingUser, AdminRoleName);
            if (!isInAdminRole)
            {
                var addRoleResult = await userManager.AddToRoleAsync(existingUser, AdminRoleName);
                if (addRoleResult.Succeeded)
                {
                    _logger.LogInformation("Assigned Admin role to existing user: {Email}", adminEmail);
                }
                else
                {
                    _logger.LogError("Failed to assign Admin role to existing user: {Errors}", string.Join(", ", addRoleResult.Errors.Select(e => e.Description)));
                }
            }
        }
    }
}