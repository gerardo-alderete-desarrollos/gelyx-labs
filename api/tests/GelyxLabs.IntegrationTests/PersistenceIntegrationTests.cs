using System.Data;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GelyxLabs.Api;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using GelyxLabs.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Testcontainers.PostgreSql;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public class DatabaseFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgresContainer;

    public DatabaseFixture()
    {
        _postgresContainer = new PostgreSqlBuilder()
            .WithImage("postgres:16-alpine")
            .WithDatabase("gelyx-labs_test")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .WithCleanUp(true)
            .Build();
    }

    public string ConnectionString => _postgresContainer.GetConnectionString();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgresContainer.StartAsync();

        var builder = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(webBuilder =>
            {
                webBuilder.UseEnvironment("Testing");
                webBuilder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<AppDbContext>((sp, options) =>
                    {
                        options.UseNpgsql(ConnectionString, npgsqlOptions =>
                        {
                            npgsqlOptions.EnableRetryOnFailure(
                                maxRetryCount: 5,
                                maxRetryDelay: TimeSpan.FromSeconds(30),
                                errorCodesToAdd: null);
                        });
                        options.UseSnakeCaseNamingConvention();
                    });
                    services.Configure<IdentityOptions>(options =>
                        options.Password.RequireNonAlphanumeric = false);
                });
                webBuilder.ConfigureAppConfiguration((context, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Default"] = ConnectionString,
                        ["MIGRATE_ON_STARTUP"] = "true",
                        ["SEED_ADMIN_EMAIL"] = "admin@test.com",
                        ["SEED_ADMIN_PASSWORD"] = "TestAdmin123!",
                        ["Jwt:Secret"] = "test-secret-key-32chars-minimal!",
                        ["Jwt:Issuer"] = "GelyxLabs",
                        ["Jwt:Audience"] = "GelyxLabs",
                        ["Auth:AllowSelfRegistration"] = "true",
                        ["App:WebBaseUrl"] = "http://localhost:3000"
                    });
                });
            });

        Factory = builder;
        _ = Factory.Server; // Trigger startup

        // Wait for hosted services (StartupInitializer) to complete initialization
        await Task.Delay(8000);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgresContainer.DisposeAsync();
    }
}

[CollectionDefinition("Database")]
public class DatabaseCollection : ICollectionFixture<DatabaseFixture> { }

[Collection("Database")]
public class PersistenceIntegrationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public PersistenceIntegrationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    private async Task<AppDbContext> GetContextAsync()
    {
        var scope = _fixture.Factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    [Fact]
    public async Task Database_MigrationAndSeed_CreatesAllTables()
    {
        using var context = await GetContextAsync();

        var tables = new[]
        {
            "users", "roles", "refresh_tokens", "modules",
            "permissions", "role_permissions", "audit_logs", "app_settings"
        };

        foreach (var table in tables)
        {
            var exists = await context.Database.SqlQuery<int>($"""SELECT 1 FROM information_schema.tables WHERE table_schema = 'public' AND table_name = {table}""").AnyAsync();
            Assert.True(exists);
        }
    }

    [Fact]
    public async Task Seed_CreatesThreeSystemRoles()
    {
        using var context = await GetContextAsync();

        var roles = await context.Roles.Where(r => r.IsSystem).ToListAsync();

        Assert.Equal(3, roles.Count);
        Assert.Contains(roles, r => r.NormalizedName == "ADMIN");
        Assert.Contains(roles, r => r.NormalizedName == "SOPORTE");
        Assert.Contains(roles, r => r.NormalizedName == "USUARIO");
    }

    [Fact]
    public async Task Seed_CreatesAdminUserWithAdminRole()
    {
        using var context = await GetContextAsync();

        var adminUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "admin@test.com");

        Assert.NotNull(adminUser);
        Assert.True(adminUser!.EmailConfirmed);
        Assert.Equal("Administrador", adminUser.FullName);
        Assert.True(adminUser.MustChangePassword);

        var adminRole = await context.Roles.FirstAsync(r => r.NormalizedName == "ADMIN");
        var hasAdminRole = await context.UserRoles.AnyAsync(ur => ur.UserId == adminUser.Id && ur.RoleId == adminRole.Id);
        Assert.True(hasAdminRole);
    }

    [Fact]
    public async Task ModuleSynchronizer_CreatesAllModulesAndPermissions()
    {
        using var context = await GetContextAsync();

        var modules = await context.Modules
            .Include(m => m.Permissions)
            .OrderBy(m => m.SortOrder)
            .ToListAsync();

        var expectedSystemModules = new[] { "users", "roles", "permissions", "audit", "settings" };
        var expectedNonSystemModules = new[] { "dashboard", "profile" };

        foreach (var key in expectedSystemModules)
        {
            var module = modules.FirstOrDefault(m => m.Key == key);
            Assert.NotNull(module);
            Assert.True(module!.IsSystem);
            Assert.True(module.IsActive);
        }

        foreach (var key in expectedNonSystemModules)
        {
            var module = modules.FirstOrDefault(m => m.Key == key);
            Assert.NotNull(module);
            Assert.False(module!.IsSystem);
            Assert.True(module.IsActive);
        }

        var allPermissions = modules.SelectMany(m => m.Permissions).ToList();

        Assert.Contains(allPermissions, p => p.Key == "users.access");
        Assert.Contains(allPermissions, p => p.Key == "users.read");
        Assert.Contains(allPermissions, p => p.Key == "users.create");
        Assert.Contains(allPermissions, p => p.Key == "users.update");
        Assert.Contains(allPermissions, p => p.Key == "users.delete");
        Assert.Contains(allPermissions, p => p.Key == "users.manage-status");
        Assert.Contains(allPermissions, p => p.Key == "users.reset-password");
        Assert.Contains(allPermissions, p => p.Key == "users.assign-roles");

        Assert.Contains(allPermissions, p => p.Key == "roles.access");
        Assert.Contains(allPermissions, p => p.Key == "roles.read");
        Assert.Contains(allPermissions, p => p.Key == "roles.create");
        Assert.Contains(allPermissions, p => p.Key == "roles.update");
        Assert.Contains(allPermissions, p => p.Key == "roles.delete");

        Assert.Contains(allPermissions, p => p.Key == "permissions.access");
        Assert.Contains(allPermissions, p => p.Key == "permissions.read");
        Assert.Contains(allPermissions, p => p.Key == "permissions.manage");

        Assert.Contains(allPermissions, p => p.Key == "audit.access");
        Assert.Contains(allPermissions, p => p.Key == "audit.read");

        Assert.Contains(allPermissions, p => p.Key == "dashboard.access");
        Assert.Contains(allPermissions, p => p.Key == "profile.access");
        Assert.Contains(allPermissions, p => p.Key == "profile.update");
    }

    [Fact]
    public async Task ModuleSynchronizer_SystemModulesOnlyAssignedToAdmin()
    {
        using var context = await GetContextAsync();

        var adminRole = await context.Roles.FirstAsync(r => r.NormalizedName == "ADMIN");
        var soporteRole = await context.Roles.FirstAsync(r => r.NormalizedName == "SOPORTE");
        var usuarioRole = await context.Roles.FirstAsync(r => r.NormalizedName == "USUARIO");

        var systemModuleKeys = new[] { "users", "roles", "permissions", "audit", "settings" };
        var nonSystemModuleKeys = new[] { "dashboard", "profile" };

        foreach (var moduleKey in systemModuleKeys)
        {
            var module = await context.Modules.FirstAsync(m => m.Key == moduleKey);
            var permissionIds = module.Permissions.Select(p => p.Id).ToList();

            var adminAssignments = await context.RolePermissions
                .Where(rp => rp.RoleId == adminRole.Id && permissionIds.Contains(rp.PermissionId))
                .ToListAsync();

            var soporteAssignments = await context.RolePermissions
                .Where(rp => rp.RoleId == soporteRole.Id && permissionIds.Contains(rp.PermissionId))
                .ToListAsync();

            var usuarioAssignments = await context.RolePermissions
                .Where(rp => rp.RoleId == usuarioRole.Id && permissionIds.Contains(rp.PermissionId))
                .ToListAsync();

            Assert.Equal(permissionIds.Count, adminAssignments.Count);
            Assert.Empty(soporteAssignments);
            Assert.Empty(usuarioAssignments);
        }

        foreach (var moduleKey in nonSystemModuleKeys)
        {
            var module = await context.Modules.FirstAsync(m => m.Key == moduleKey);
            var permissionIds = module.Permissions.Select(p => p.Id).ToList();

            foreach (var role in new[] { adminRole, soporteRole, usuarioRole })
            {
                var assignments = await context.RolePermissions
                    .Where(rp => rp.RoleId == role.Id && permissionIds.Contains(rp.PermissionId))
                    .ToListAsync();

                Assert.Equal(permissionIds.Count, assignments.Count);
            }
        }
    }

    [Fact]
    public async Task HealthEndpoint_ReturnsHealthy()
    {
        var client = _fixture.Factory.CreateClient();
        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("healthy", content);
    }

    [Fact]
    public async Task XminConcurrency_ThrowsOnConflictingUpdates()
    {
        using var context1 = await GetContextAsync();
        using var context2 = await GetContextAsync();

        var user = new AppUser
        {
            UserName = "concurrency@test.com",
            Email = "concurrency@test.com",
            EmailConfirmed = true,
            FullName = "Concurrency Test",
            IsActive = true,
            MustChangePassword = false
        };
        context1.Users.Add(user);
        await context1.SaveChangesAsync();

        var userId = user.Id;

        var user1 = await context1.Users.FirstAsync(u => u.Id == userId);
        var user2 = await context2.Users.FirstAsync(u => u.Id == userId);

        user1.FullName = "Updated by Context 1";
        user2.FullName = "Updated by Context 2";

        await context1.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(async () =>
        {
            await context2.SaveChangesAsync();
        });
    }
}
