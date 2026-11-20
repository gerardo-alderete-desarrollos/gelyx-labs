using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public class PermissionsEndpointsTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public PermissionsEndpointsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Admin_CanReadPermissionModulesAndMatrix()
    {
        using var client = await CreateAdminClientAsync();

        using var modulesResponse = await client.GetAsync("/api/v1/permissions/modules");
        using var modules = JsonDocument.Parse(await modulesResponse.Content.ReadAsStringAsync());
        modulesResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        modules.RootElement.EnumerateArray().Should().Contain(module =>
            module.GetProperty("key").GetString() == "users" &&
            module.GetProperty("permissions").GetArrayLength() > 0);

        using var matrixResponse = await client.GetAsync("/api/v1/permissions/matrix");
        using var matrix = JsonDocument.Parse(await matrixResponse.Content.ReadAsStringAsync());
        matrixResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        matrix.RootElement.GetProperty("roles").EnumerateArray()
            .Should().Contain(role => role.GetProperty("name").GetString() == "Admin");
        matrix.RootElement.GetProperty("modules").EnumerateArray()
            .Should().Contain(module => module.GetProperty("key").GetString() == "users");
    }

    [Theory]
    [InlineData("Soporte")]
    [InlineData("Usuario")]
    public async Task NonAdmin_IsForbiddenFromAllPermissionEndpoints(string role)
    {
        using var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: role);
        await SetBearerTokenAsync(client, user.Email, user.Password);
        var roleId = await FindRoleIdAsync(role);

        var responses = new[]
        {
            await client.GetAsync("/api/v1/permissions/modules"),
            await client.GetAsync("/api/v1/permissions/matrix"),
            await client.GetAsync($"/api/v1/permissions/roles/{roleId}"),
            await client.PutAsync($"/api/v1/permissions/roles/{roleId}", JsonContent(new[] { "dashboard.access" }))
        };
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.Dispose();
        }
    }

    [Fact]
    public async Task UpdatingRoleWithSystemPermissionReturns422()
    {
        using var client = await CreateAdminClientAsync();
        var roleId = await FindRoleIdAsync("Soporte");

        using var response = await client.PutAsync(
            $"/api/v1/permissions/roles/{roleId}",
            JsonContent(new[] { "users.read" }));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertCodeAsync(response, "system_permissions_restricted");
    }

    [Fact]
    public async Task UpdatingAdminPermissionsReturns409()
    {
        using var client = await CreateAdminClientAsync();
        var roleId = await FindRoleIdAsync("Admin");

        using var response = await client.PutAsync(
            $"/api/v1/permissions/roles/{roleId}",
            JsonContent(Array.Empty<string>()));

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        await AssertCodeAsync(response, "admin_role_protected");
    }

    [Fact]
    public async Task RemovingModuleAccessAlsoRemovesItsActions()
    {
        using var client = await CreateAdminClientAsync();
        var role = await TestUserFactory.CreateAsync(_fixture.Factory, role: $"Matrix-{Guid.NewGuid():N}");
        var roleId = await FindRoleIdAsyncByUser(role.Email);
        await PutPermissionsAsync(client, roleId, ["profile.access", "profile.update"]);

        using var response = await client.PutAsync(
            $"/api/v1/permissions/roles/{roleId}",
            JsonContent(Array.Empty<string>()));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var roleResponse = await client.GetAsync($"/api/v1/permissions/roles/{roleId}");
        using var document = JsonDocument.Parse(await roleResponse.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("permissions").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task GrantingActionWithoutAccessAutomaticallyGrantsAccessAndUpdatesMe()
    {
        using var client = await CreateAdminClientAsync();
        var roleName = $"Matrix-{Guid.NewGuid():N}";
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: roleName);
        var roleId = await FindRoleIdAsync(roleName);
        await PutPermissionsAsync(client, roleId, ["profile.update"]);

        using var roleResponse = await client.GetAsync($"/api/v1/permissions/roles/{roleId}");
        using var roleDocument = JsonDocument.Parse(await roleResponse.Content.ReadAsStringAsync());
        roleDocument.RootElement.GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .Should().Contain(["profile.access", "profile.update"]);

        using var userClient = _fixture.Factory.CreateClient();
        await SetBearerTokenAsync(userClient, user.Email, user.Password);
        using var meResponse = await userClient.GetAsync("/api/v1/auth/me");
        using var me = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        me.RootElement.GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .Should().Contain(["profile.access", "profile.update"]);
    }

    private async Task<HttpClient> CreateAdminClientAsync()
    {
        var client = _fixture.Factory.CreateClient();
        var admin = await TestUserFactory.CreateAsync(_fixture.Factory, role: "Admin");
        await SetBearerTokenAsync(client, admin.Email, admin.Password);
        return client;
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
        return await context.Roles
            .Where(role => role.Name == roleName)
            .Select(role => role.Id)
            .SingleAsync();
    }

    private async Task<Guid> FindRoleIdAsyncByUser(string email)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<AppUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        var roles = await userManager.GetRolesAsync(user!);
        return await FindRoleIdAsync(roles.Single());
    }

    private static StringContent JsonContent(string[] permissions) =>
        new(JsonSerializer.Serialize(new { permissions }), Encoding.UTF8, "application/json");

    private static async Task AssertCodeAsync(HttpResponseMessage response, string code)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be(code);
    }

    [Fact]
    public async Task RoleGetEndpoint_ReturnsAssignedPermissions()
    {
        using var client = await CreateAdminClientAsync();
        var roleName = $"Matrix-{Guid.NewGuid():N}";
        await TestUserFactory.CreateAsync(_fixture.Factory, role: roleName);
        var roleId = await FindRoleIdAsync(roleName);
        await PutPermissionsAsync(client, roleId, ["dashboard.access"]);

        using var response = await client.GetAsync($"/api/v1/permissions/roles/{roleId}");
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        document.RootElement.GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .Should().Contain("dashboard.access");
    }

    private static async Task PutPermissionsAsync(HttpClient client, Guid roleId, string[] permissions)
    {
        using var response = await client.PutAsync(
            $"/api/v1/permissions/roles/{roleId}",
            JsonContent(permissions));
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, await response.Content.ReadAsStringAsync());
    }
}
