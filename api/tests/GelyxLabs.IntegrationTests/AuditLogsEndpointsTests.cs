using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GelyxLabs.Application.Abstractions.Audit;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GelyxLabs.IntegrationTests;

[Collection("Database")]
public sealed class AuditLogsEndpointsTests
{
    private readonly DatabaseFixture _fixture;

    public AuditLogsEndpointsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Admin_CanListAndFilterAuditLogsAndSuccessfulPermissionUpdateIsLogged()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var roleName = $"Audit-{Guid.NewGuid():N}";
        await TestUserFactory.CreateAsync(_fixture.Factory, role: roleName);
        var roleId = await FindRoleIdAsync(roleName);

        using var update = await client.PutAsync(
            $"/api/v1/permissions/roles/{roleId}",
            JsonContent(["profile.update"]));
        update.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var actorUserId = await FindUserIdAsync(admin.Email);
        using var filteredResponse = await client.GetAsync(
            $"/api/v1/audit-logs?page=1&pageSize=10&entityType=role&action=permissions.role-updated&actorUserId={actorUserId}");
        filteredResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var filtered = JsonDocument.Parse(await filteredResponse.Content.ReadAsStringAsync());
        var root = filtered.RootElement;
        root.GetProperty("page").GetInt32().Should().Be(1);
        root.GetProperty("pageSize").GetInt32().Should().Be(10);
        root.GetProperty("totalCount").GetInt32().Should().Be(1);
        root.GetProperty("totalPages").GetInt32().Should().Be(1);
        var item = root.GetProperty("items").EnumerateArray().Single();
        item.GetProperty("action").GetString().Should().Be("permissions.role-updated");
        item.GetProperty("entityType").GetString().Should().Be("role");
        item.GetProperty("entityId").GetString().Should().Be(roleId.ToString());
        item.GetProperty("actorUserId").GetGuid().Should().Be(actorUserId);
        var metadata = item.GetProperty("metadata");
        metadata.GetProperty("roleId").GetGuid().Should().Be(roleId);
        metadata.GetProperty("granted").EnumerateArray()
            .Select(permission => permission.GetString()).Should().Contain(["profile.access", "profile.update"]);
        metadata.GetProperty("removed").GetArrayLength().Should().Be(0);

        using var response = await client.GetAsync("/api/v1/audit-logs?pageSize=1000");
        using var page = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        page.RootElement.GetProperty("pageSize").GetInt32().Should().Be(100);
        page.RootElement.GetProperty("items").GetArrayLength().Should().BeLessThanOrEqualTo(100);
    }

    [Fact]
    public async Task Admin_CanExportFilteredAuditLogsWithFormulaNeutralization()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var actorUserId = await FindUserIdAsync(admin.Email);
        var entityType = $"export-filter-{Guid.NewGuid():N}";
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Action = "=HYPERLINK(\"https://example.com\")",
                EntityType = entityType,
                Metadata = "{\"source\":\"test\"}",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
        }

        using var response = await client.GetAsync(
            $"/api/v1/audit-logs/export?entityType={Uri.EscapeDataString(entityType)}&actorUserId={actorUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentDisposition!.FileNameStar.Should().Be("audit-logs.csv");
        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().Contain(entityType);
        csv.Should().Contain("\"'=HYPERLINK(\"\"https://example.com\"\")\"");
        csv.Should().NotContain("permissions.role-updated");
    }

    [Theory]
    [InlineData("\t")]
    [InlineData("\r")]
    [InlineData("\n")]
    [InlineData("\u0001")]
    [InlineData(" \t\r\n")]
    public async Task Admin_CsvExportNeutralizesFormulasAfterLeadingWhitespaceOrControlCharacters(string prefix)
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var actorUserId = await FindUserIdAsync(admin.Email);
        var entityType = $"formula-prefix-{Guid.NewGuid():N}";
        var action = $"{prefix}=SUM(1,1)";
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                ActorUserId = actorUserId,
                Action = action,
                EntityType = entityType,
                Metadata = "{}",
                CreatedAt = DateTimeOffset.UtcNow
            });
            await context.SaveChangesAsync();
        }

        using var response = await client.GetAsync(
            $"/api/v1/audit-logs/export?entityType={Uri.EscapeDataString(entityType)}&actorUserId={actorUserId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain($"\"'{action}\"");
    }

    [Theory]
    [InlineData("Soporte")]
    [InlineData("Usuario")]
    public async Task NonAdmin_CannotListAuditLogs(string role)
    {
        using var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: role);
        await SetBearerTokenAsync(client, user.Email, user.Password);

        using var response = await client.GetAsync("/api/v1/audit-logs");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task FailedPermissionUpdates_DoNotCreateAuditLogs()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var actorUserId = await FindUserIdAsync(admin.Email);
        var roleId = await FindRoleIdAsync("Admin");
        var initialCount = await CountRoleUpdateLogsAsync(actorUserId);

        using var conflict = await client.PutAsync(
            $"/api/v1/permissions/roles/{roleId}",
            JsonContent([]));
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var soporteRoleId = await FindRoleIdAsync("Soporte");
        using var invalid = await client.PutAsync(
            $"/api/v1/permissions/roles/{soporteRoleId}",
            JsonContent(["users.read"]));
        invalid.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        (await CountRoleUpdateLogsAsync(actorUserId)).Should().Be(initialCount);
    }

    [Fact]
    public async Task MetadataSanitizer_RemovesSensitiveKeysIgnoringCaseAtEveryDepth()
    {
        var entityId = Guid.NewGuid().ToString();
        using var scope = _fixture.Factory.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<IAuditLogger>();
        await logger.LogAsync(
            "audit.sanitizer-test",
            "test",
            entityId,
            new Dictionary<string, object?>
            {
                ["Password"] = "not-persisted-password",
                ["accessTOKEN"] = "not-persisted-token",
                ["requestHash"] = "not-persisted-hash",
                ["clientSecret"] = "not-persisted-secret",
                ["safe"] = "retained",
                ["nested"] = new Dictionary<string, object?>
                {
                    ["RefreshTokenValue"] = "nested-token",
                    ["safeNested"] = "retained"
                },
                ["entries"] = new[]
                {
                    new Dictionary<string, object?> { ["secretValue"] = "array-secret", ["name"] = "kept" }
                }
            });

        using var verifyScope = _fixture.Factory.Services.CreateScope();
        var context = verifyScope.ServiceProvider.GetRequiredService<AppDbContext>();
        var entry = await context.AuditLogs.SingleAsync(log => log.EntityId == entityId);
        using var metadata = JsonDocument.Parse(entry.Metadata);
        metadata.RootElement.GetProperty("safe").GetString().Should().Be("retained");
        metadata.RootElement.GetProperty("nested").GetProperty("safeNested").GetString().Should().Be("retained");
        metadata.RootElement.GetProperty("entries")[0].GetProperty("name").GetString().Should().Be("kept");
        GetPropertyNames(metadata.RootElement).Should().NotContain(name =>
            new[] { "password", "token", "hash", "secret" }
                .Any(part => name.Contains(part, StringComparison.OrdinalIgnoreCase)));
        var sanitizedJson = metadata.RootElement.GetRawText();
        sanitizedJson.Should().NotContain("not-persisted");
        sanitizedJson.Should().NotContain("nested-token");
        sanitizedJson.Should().NotContain("array-secret");
    }

    private async Task<(HttpClient Client, string Email)> CreateAdminClientAsync()
    {
        var admin = await TestUserFactory.CreateAsync(_fixture.Factory, role: "Admin");
        var client = _fixture.Factory.CreateClient();
        await SetBearerTokenAsync(client, admin.Email, admin.Password);
        return (client, admin.Email);
    }

    private async Task SetBearerTokenAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email, password }), Encoding.UTF8, "application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            document.RootElement.GetProperty("accessToken").GetString());
    }

    private async Task<Guid> FindRoleIdAsync(string roleName)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Roles.Where(role => role.Name == roleName).Select(role => role.Id).SingleAsync();
    }

    private async Task<Guid> FindUserIdAsync(string email)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }

    private async Task<int> CountRoleUpdateLogsAsync(Guid actorUserId)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.AuditLogs.CountAsync(log =>
            log.ActorUserId == actorUserId && log.Action == "permissions.role-updated");
    }

    private static StringContent JsonContent(string[] permissions) =>
        new(JsonSerializer.Serialize(new { permissions }), Encoding.UTF8, "application/json");

    private static IEnumerable<string> GetPropertyNames(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                yield return property.Name;
                foreach (var nestedName in GetPropertyNames(property.Value))
                {
                    yield return nestedName;
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                foreach (var nestedName in GetPropertyNames(item))
                {
                    yield return nestedName;
                }
            }
        }
    }
}
