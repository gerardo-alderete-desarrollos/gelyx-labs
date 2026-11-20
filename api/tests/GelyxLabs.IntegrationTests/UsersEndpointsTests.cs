using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using GelyxLabs.Application.Features.Users;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GelyxLabs.IntegrationTests;

[Collection("Database")]
public sealed class UsersEndpointsTests
{
    private readonly DatabaseFixture _fixture;

    public UsersEndpointsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Admin_CanCreateListUpdateDisableDeleteAndRestoreUsers()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        using var meResponse = await client.GetAsync("/api/v1/auth/me");
        using var me = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        me.RootElement.GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .Should().Contain(["users.create", "permissions.manage"]);
        var roleName = $"UsersFlow-{Guid.NewGuid():N}";
        await TestUserFactory.CreateAsync(_fixture.Factory, role: roleName);
        var roleId = await FindRoleIdAsync(roleName);
        var email = $"managed.{Guid.NewGuid():N}@example.com";

        using var createResponse = await client.PostAsync(
            "/api/v1/users",
            JsonContent(new { email, fullName = "Managed User", roleIds = new[] { roleId } }));
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created, await createResponse.Content.ReadAsStringAsync());
        using var created = JsonDocument.Parse(await createResponse.Content.ReadAsStringAsync());
        var userId = created.RootElement.GetProperty("id").GetGuid();
        var temporaryPassword = created.RootElement.GetProperty("temporaryPassword").GetString();
        temporaryPassword.Should().NotBeNullOrWhiteSpace();

        using var listResponse = await client.GetAsync(
            $"/api/v1/users?search=managed&roleId={roleId}&status=active&page=1&pageSize=10");
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var list = JsonDocument.Parse(await listResponse.Content.ReadAsStringAsync());
        list.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);
        list.RootElement.GetProperty("items")[0].GetProperty("email").GetString().Should().Be(email);

        using var detailsResponse = await client.GetAsync($"/api/v1/users/{userId}");
        using var details = JsonDocument.Parse(await detailsResponse.Content.ReadAsStringAsync());
        detailsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        details.RootElement.GetProperty("mustChangePassword").GetBoolean().Should().BeTrue();
        var rowVersion = details.RootElement.GetProperty("rowVersion").GetUInt32();

        using var updateResponse = await client.PutAsync(
            $"/api/v1/users/{userId}",
            JsonContent(new
            {
                email,
                fullName = "Managed User Updated",
                phoneNumber = "+15551234567",
                rowVersion
            }));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var staleUpdateResponse = await client.PutAsync(
            $"/api/v1/users/{userId}",
            JsonContent(new
            {
                email,
                fullName = "Stale Update",
                phoneNumber = "+15551234567",
                rowVersion
            }));
        staleUpdateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var defaultRoleId = await FindRoleIdAsync("Usuario");
        using var rolesResponse = await client.PutAsync(
            $"/api/v1/users/{userId}/roles",
            JsonContent(new { roleIds = new[] { roleId, defaultRoleId } }));
        rolesResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var resetResponse = await client.PostAsync($"/api/v1/users/{userId}/reset-password", null);
        using var reset = JsonDocument.Parse(await resetResponse.Content.ReadAsStringAsync());
        resetResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        reset.RootElement.GetProperty("temporaryPassword").GetString().Should().NotBeNullOrWhiteSpace();

        using var statusResponse = await client.PatchAsync(
            $"/api/v1/users/{userId}/status",
            JsonContent(new { isActive = false }));
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var deleteResponse = await client.DeleteAsync($"/api/v1/users/{userId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var deletedListResponse = await client.GetAsync($"/api/v1/users?status=deleted&search=managed");
        using var deletedList = JsonDocument.Parse(await deletedListResponse.Content.ReadAsStringAsync());
        deletedList.RootElement.GetProperty("totalCount").GetInt32().Should().Be(1);

        using var restoreResponse = await client.PostAsync($"/api/v1/users/{userId}/restore", null);
        restoreResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var summaryResponse = await client.GetAsync("/api/v1/users/summary");
        using var summary = JsonDocument.Parse(await summaryResponse.Content.ReadAsStringAsync());
        summaryResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        summary.RootElement.GetProperty("total").GetInt32().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Admin_CanExportFilteredUsersWithFormulaNeutralization()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var roleId = await FindRoleIdAsync("Usuario");
        var email = $"export.{Guid.NewGuid():N}@example.com";
        using var create = await client.PostAsync(
            "/api/v1/users",
            JsonContent(new { email, fullName = "=SUM(1,1)", roleIds = new[] { roleId } }));
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        using var response = await client.GetAsync($"/api/v1/users/export?search={Uri.EscapeDataString(email)}&status=active");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("text/csv");
        response.Content.Headers.ContentType.CharSet.Should().Be("utf-8");
        response.Content.Headers.ContentDisposition!.FileNameStar.Should().Be("users.csv");
        var csv = await response.Content.ReadAsStringAsync();
        csv.Should().Contain("\"Id\",\"Email\",\"FullName\"");
        csv.Should().Contain($"\"{email}\"");
        csv.Should().Contain("\"'=SUM(1,1)\"");
        csv.Should().NotContain("Admin");
    }

    [Theory]
    [InlineData("Soporte")]
    [InlineData("Usuario")]
    public async Task NonAdmin_ReceivesForbiddenForEveryUserPermissionAndAuditEndpoint(string role)
    {
        using var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: role);
        await SetBearerTokenAsync(client, user.Email, user.Password);
        var roleId = await FindRoleIdAsync(role);
        var userId = await FindUserIdAsync(user.Email);

        var responses = new[]
        {
            await client.GetAsync("/api/v1/users"),
            await client.GetAsync("/api/v1/users/export"),
            await client.GetAsync("/api/v1/users/summary"),
            await client.GetAsync($"/api/v1/users/{userId}"),
            await client.PostAsync("/api/v1/users", JsonContent(new { email = "forbidden@example.com", fullName = "Forbidden", roleIds = new[] { roleId } })),
            await client.PutAsync($"/api/v1/users/{userId}", JsonContent(new { email = user.Email, fullName = "Forbidden", phoneNumber = "", rowVersion = 0 })),
            await client.PatchAsync($"/api/v1/users/{userId}/status", JsonContent(new { isActive = false })),
            await client.PutAsync($"/api/v1/users/{userId}/roles", JsonContent(new { roleIds = new[] { roleId } })),
            await client.PostAsync($"/api/v1/users/{userId}/reset-password", null),
            await client.DeleteAsync($"/api/v1/users/{userId}"),
            await client.PostAsync($"/api/v1/users/{userId}/restore", null),
            await client.GetAsync("/api/v1/permissions/modules"),
            await client.GetAsync("/api/v1/permissions/matrix"),
            await client.GetAsync($"/api/v1/permissions/roles/{roleId}"),
            await client.PutAsync($"/api/v1/permissions/roles/{roleId}", JsonContent(new { permissions = new[] { "dashboard.access" } })),
            await client.GetAsync("/api/v1/audit-logs"),
            await client.GetAsync("/api/v1/audit-logs/export")
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.Dispose();
        }
    }

    [Fact]
    public async Task Admin_CannotAssignSystemPermissionToSupportRole()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var soporteRoleId = await FindRoleIdAsync("Soporte");

        using var response = await client.PutAsync(
            $"/api/v1/permissions/roles/{soporteRoleId}",
            JsonContent(new { permissions = new[] { "users.read" } }));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Admin_CannotDeleteOrDeactivateThemself()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var userId = await FindUserIdAsync(admin.Email);

        using var deactivate = await client.PatchAsync(
            $"/api/v1/users/{userId}/status",
            JsonContent(new { isActive = false }));
        using var delete = await client.DeleteAsync($"/api/v1/users/{userId}");

        deactivate.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        delete.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EmailUniquenessIncludesSoftDeletedAccountsAndIgnoresCase()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var roleId = await FindRoleIdAsync("Usuario");
        var email = $"deleted.{Guid.NewGuid():N}@example.com";
        using var create = await client.PostAsync(
            "/api/v1/users",
            JsonContent(new { email, fullName = "Deleted Email", roleIds = new[] { roleId } }));
        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var userId = created.RootElement.GetProperty("id").GetGuid();
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        using var delete = await client.DeleteAsync($"/api/v1/users/{userId}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var duplicate = await client.PostAsync(
            "/api/v1/users",
            JsonContent(new { email = email.ToUpperInvariant(), fullName = "Duplicate", roleIds = new[] { roleId } }));
        duplicate.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ConcurrentUserUpdateIsReturnedAsConcurrencyFailure()
    {
        var created = await TestUserFactory.CreateAsync(_fixture.Factory);
        var userId = await FindUserIdAsync(created.Email);
        using var staleScope = _fixture.Factory.Services.CreateScope();
        var repository = staleScope.ServiceProvider.GetRequiredService<IUsersRepository>();
        var staleUser = await repository.GetTrackedUserAsync(userId, false, CancellationToken.None);
        staleUser.Should().NotBeNull();
        var expectedRowVersion = staleUser!.RowVersion;

        using (var concurrentScope = _fixture.Factory.Services.CreateScope())
        {
            var userManager = concurrentScope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var concurrentUser = await userManager.FindByIdAsync(userId.ToString());
            concurrentUser.Should().NotBeNull();
            concurrentUser!.FullName = "Concurrent update";
            var updateResult = await userManager.UpdateAsync(concurrentUser);
            updateResult.Succeeded.Should().BeTrue();
        }

        staleUser.FullName = "Stale update";
        var result = await repository.UpdateUserAsync(staleUser, expectedRowVersion, CancellationToken.None);

        result.Succeeded.Should().BeFalse();
        result.Errors.Should().Contain(error => error.Code == "ConcurrencyFailure");
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
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await manager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }

    private static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
