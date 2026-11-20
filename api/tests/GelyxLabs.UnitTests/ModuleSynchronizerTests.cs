using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GelyxLabs.Application;
using GelyxLabs.Application.Abstractions.Security;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using GelyxLabs.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GelyxLabs.UnitTests;

public class ConfigurableModuleDefinition : IModuleDefinition
{
    public string Key { get; set; } = "test";
    public string Name { get; set; } = "Test Module";
    public string? Description { get; set; } = "Test description";
    public IReadOnlyDictionary<string, ModuleTranslation> Translations { get; set; } =
        new Dictionary<string, ModuleTranslation>
        {
            ["es"] = new() { Name = "Módulo de prueba", Description = "Descripción de prueba" },
            ["en"] = new() { Name = "Test module", Description = "Test description" }
        };
    public string? Icon { get; set; } = "test";
    public string? Route { get; set; } = "/test";
    public int SortOrder { get; set; } = 1;
    public bool IsSystem { get; set; } = false;
    public List<(string Permission, string Description)> Permissions { get; set; } = new();

    public IEnumerable<(string Permission, string Description)> GetPermissions()
    {
        return Permissions;
    }
}

public class ModuleSynchronizerTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;

        var interceptor = new GelyxLabs.Infrastructure.Persistence.Interceptors.AuditableEntityInterceptor();
        var context = new AppDbContext(options, interceptor);
        context.Database.EnsureCreated();
        return context;
    }

    private static IModuleSynchronizer CreateSynchronizer(
        AppDbContext context,
        IEnumerable<IModuleDefinition> definitions,
        ILogger<ModuleSynchronizer>? logger = null)
    {
        var serviceProvider = new ServiceCollection()
            .AddLogging()
            .BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();

        var syncLogger = logger ?? loggerFactory.CreateLogger<ModuleSynchronizer>();

        return new ModuleSynchronizer(context, definitions, syncLogger, new TestPermissionService());
    }

    private sealed class TestPermissionService : IPermissionService
    {
        public Task<IReadOnlyCollection<string>> GetPermissionsAsync(
            Guid userId,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyCollection<string>>(Array.Empty<string>());

        public void InvalidateUser(Guid userId) { }

        public void InvalidateAll() { }
    }

    private static ConfigurableModuleDefinition CreateTestModule(
        string key = "test",
        string name = "Test Module",
        bool isSystem = false,
        IEnumerable<(string Permission, string Description)>? permissions = null)
    {
        return new ConfigurableModuleDefinition
        {
            Key = key,
            Name = name,
            IsSystem = isSystem,
            Permissions = permissions?.ToList() ?? new List<(string, string)>
            {
                ($"{key}.access", $"Access {key} module"),
                ($"{key}.read", $"Read {key}"),
                ($"{key}.write", $"Write {key}")
            }
        };
    }

    [Fact]
    public async Task SynchronizeAsync_CreatesNewModulesAndPermissions()
    {
        using var context = CreateContext();
        var definitions = new List<IModuleDefinition>
        {
            CreateTestModule("test", "Test Module", false),
            CreateTestModule("testsystem", "Test System Module", true, new[]
            {
                ("testsystem.access", "Access test system"),
                ("testsystem.read", "Read test system")
            })
        };

        var synchronizer = CreateSynchronizer(context, definitions);
        await synchronizer.SynchronizeAsync();

        var modules = context.Modules.ToList();
        Assert.Equal(2, modules.Count);

        var testModule = modules.First(m => m.Key == "test");
        Assert.False(testModule.IsSystem);
        Assert.Equal(3, testModule.Permissions.Count);

        var systemModule = modules.First(m => m.Key == "testsystem");
        Assert.True(systemModule.IsSystem);
        Assert.Equal(2, systemModule.Permissions.Count);
    }

    [Fact]
    public async Task SynchronizeAsync_IsIdempotent_RunningTwiceProducesSameResult()
    {
        using var context = CreateContext();
        var definitions = new List<IModuleDefinition>
        {
            CreateTestModule()
        };

        var synchronizer = CreateSynchronizer(context, definitions);

        await synchronizer.SynchronizeAsync();
        var firstCount = context.Modules.Count();
        var firstPermCount = context.Permissions.Count();

        await synchronizer.SynchronizeAsync();
        var secondCount = context.Modules.Count();
        var secondPermCount = context.Permissions.Count();

        Assert.Equal(firstCount, secondCount);
        Assert.Equal(firstPermCount, secondPermCount);
    }

    [Fact]
    public async Task SynchronizeAsync_DoesNotRemoveExistingRolePermissionAssignments()
    {
        using var context = CreateContext();
        var definitions = new List<IModuleDefinition>
        {
            CreateTestModule()
        };

        var synchronizer = CreateSynchronizer(context, definitions);
        await synchronizer.SynchronizeAsync();

        var module = context.Modules.First(m => m.Key == "test");
        var permission = module.Permissions.First(p => p.Key == "test.access");

        var adminRole = new AppRole
        {
            Name = "Admin",
            NormalizedName = "ADMIN",
            IsSystem = true
        };
        context.Roles.Add(adminRole);
        await context.SaveChangesAsync();

        var rolePermission = new RolePermission
        {
            RoleId = adminRole.Id,
            PermissionId = permission.Id
        };
        context.RolePermissions.Add(rolePermission);
        await context.SaveChangesAsync();

        await synchronizer.SynchronizeAsync();

        var stillAssigned = context.RolePermissions.Any(rp => rp.RoleId == adminRole.Id && rp.PermissionId == permission.Id);
        Assert.True(stillAssigned, "Existing RolePermission assignment should not be removed");
    }

    [Fact]
    public async Task SynchronizeAsync_NewPermissionsAssignedOnlyToAdmin()
    {
        using var context = CreateContext();

        var adminRole = new AppRole { Name = "Admin", NormalizedName = "ADMIN", IsSystem = true };
        var userRole = new AppRole { Name = "User", NormalizedName = "USER", IsSystem = true };
        context.Roles.AddRange(adminRole, userRole);
        await context.SaveChangesAsync();

        var definitions = new List<IModuleDefinition>
        {
            CreateTestModule("test", "Test", false, new[]
            {
                ("test.access", "Access"),
                ("test.read", "Read"),
                ("test.write", "Write")
            })
        };

        var synchronizer = CreateSynchronizer(context, definitions);
        await synchronizer.SynchronizeAsync();

        var writePermission = context.Permissions.First(p => p.Key == "test.write");
        var adminHasWrite = context.RolePermissions.Any(rp => rp.RoleId == adminRole.Id && rp.PermissionId == writePermission.Id);
        var userHasWrite = context.RolePermissions.Any(rp => rp.RoleId == userRole.Id && rp.PermissionId == writePermission.Id);

        Assert.True(adminHasWrite, "New permission should be assigned to Admin");
        Assert.False(userHasWrite, "New permission should NOT be assigned to non-Admin");
    }

    [Fact]
    public async Task SynchronizeAsync_SystemModulesNeverAssignedToNonAdmin()
    {
        using var context = CreateContext();

        var adminRole = new AppRole { Name = "Admin", NormalizedName = "ADMIN", IsSystem = true };
        var userRole = new AppRole { Name = "User", NormalizedName = "USER", IsSystem = true };
        context.Roles.AddRange(adminRole, userRole);
        await context.SaveChangesAsync();

        var definitions = new List<IModuleDefinition>
        {
            CreateTestModule("testsystem", "Test System", true, new[]
            {
                ("testsystem.access", "Access"),
                ("testsystem.read", "Read"),
                ("testsystem.new", "New")
            })
        };

        var synchronizer = CreateSynchronizer(context, definitions);
        await synchronizer.SynchronizeAsync();

        var newPermission = context.Permissions.First(p => p.Key == "testsystem.new");
        var adminHasNew = context.RolePermissions.Any(rp => rp.RoleId == adminRole.Id && rp.PermissionId == newPermission.Id);
        var userHasNew = context.RolePermissions.Any(rp => rp.RoleId == userRole.Id && rp.PermissionId == newPermission.Id);

        Assert.True(adminHasNew, "System module permission should be assigned to Admin");
        Assert.False(userHasNew, "System module permission should NEVER be assigned to non-Admin");
    }

    [Fact]
    public async Task SynchronizeAsync_ModuleRemovedFromCode_MarkedInactive()
    {
        using var context = CreateContext();
        var definitions = new List<IModuleDefinition>
        {
            CreateTestModule("oldmodule", "Old Module")
        };

        var synchronizer = CreateSynchronizer(context, definitions);
        await synchronizer.SynchronizeAsync();

        var oldModule = context.Modules.First(m => m.Key == "oldmodule");
        Assert.True(oldModule.IsActive);

        var newDefinitions = new List<IModuleDefinition>
        {
            CreateTestModule("newmodule", "New Module")
        };

        var newSynchronizer = CreateSynchronizer(context, newDefinitions);
        await newSynchronizer.SynchronizeAsync();

        var stillExists = context.Modules.Any(m => m.Key == "oldmodule");
        Assert.True(stillExists, "Module should still exist but be inactive");

        var oldModuleAfter = context.Modules.First(m => m.Key == "oldmodule");
        Assert.False(oldModuleAfter.IsActive, "Module no longer in code should be marked IsActive=false");
    }
}