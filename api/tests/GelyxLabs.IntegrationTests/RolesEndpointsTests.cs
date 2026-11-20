using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GelyxLabs.IntegrationTests;

[Collection("Database")]
public sealed class RolesEndpointsTests
{
    private readonly DatabaseFixture _fixture;

    public RolesEndpointsTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Admin_CreatesCopiesAssignsAndDeletesRoleWithReassignment()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var soporteRoleId = await FindRoleIdAsync("Soporte");
        var supervisorName = $"Supervisor-{Guid.NewGuid():N}";

        using var setSourcePermissions = await client.PutAsync(
            $"/api/v1/permissions/roles/{soporteRoleId}",
            JsonContent(new { permissions = new[] { "profile.access" } }));
        setSourcePermissions.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var create = await client.PostAsync(
            "/api/v1/roles",
            JsonContent(new { name = supervisorName, description = "Copied role", copyPermissionsFromRoleId = soporteRoleId }));
        using var restoreSourcePermissions = await client.PutAsync(
            $"/api/v1/permissions/roles/{soporteRoleId}",
            JsonContent(new { permissions = Array.Empty<string>() }));
        restoreSourcePermissions.StatusCode.Should().Be(HttpStatusCode.NoContent);
        create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());

        using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
        var supervisorId = created.RootElement.GetProperty("id").GetGuid();
        created.RootElement.GetProperty("isSystem").GetBoolean().Should().BeFalse();
        var permissions = created.RootElement.GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .ToArray();
        permissions.Should().Equal("profile.access");
        permissions.Select(permission => permission!).Should().OnlyContain(permission =>
            !new[] { "users.", "roles.", "permissions.", "audit." }
                .Any(prefix => permission.StartsWith(prefix, StringComparison.Ordinal)));

        var user = await TestUserFactory.CreateAsync(_fixture.Factory);
        var userId = await FindUserIdAsync(user.Email);
        using var assign = await client.PutAsync(
            $"/api/v1/users/{userId}/roles",
            JsonContent(new { roleIds = new[] { supervisorId } }));
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var refreshTokenId = Guid.NewGuid();
        using (var tokenScope = _fixture.Factory.Services.CreateScope())
        {
            var context = tokenScope.ServiceProvider.GetRequiredService<AppDbContext>();
            context.RefreshTokens.Add(new RefreshToken
            {
                Id = refreshTokenId,
                UserId = userId,
                TokenHash = $"test-hash-{Guid.NewGuid():N}",
                CreatedAt = DateTimeOffset.UtcNow,
                ExpiresAt = DateTimeOffset.UtcNow.AddDays(7)
            });
            await context.SaveChangesAsync();
        }

        using var matrixResponse = await client.GetAsync("/api/v1/permissions/matrix");
        using var matrix = JsonDocument.Parse(await matrixResponse.Content.ReadAsStringAsync());
        matrix.RootElement.GetProperty("roles").EnumerateArray()
            .Should().Contain(role => role.GetProperty("id").GetGuid() == supervisorId);

        using var conflict = await client.DeleteAsync($"/api/v1/roles/{supervisorId}");
        conflict.StatusCode.Should().Be(HttpStatusCode.Conflict);
        using (var problem = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync()))
        {
            problem.RootElement.GetProperty("code").GetString().Should().Be("role_has_users");
        }

        using var reassign = await client.DeleteAsync(
            $"/api/v1/roles/{supervisorId}?reassignToRoleId={await FindRoleIdAsync("Usuario")}");
        reassign.StatusCode.Should().Be(HttpStatusCode.NoContent, await reassign.Content.ReadAsStringAsync());

        using var roleListResponse = await client.GetAsync("/api/v1/roles");
        using var roleList = JsonDocument.Parse(await roleListResponse.Content.ReadAsStringAsync());
        roleList.RootElement.EnumerateArray()
            .Should().NotContain(role => role.GetProperty("id").GetGuid() == supervisorId);

        using var userDetails = await client.GetAsync($"/api/v1/users/{userId}");
        using var userDocument = JsonDocument.Parse(await userDetails.Content.ReadAsStringAsync());
        userDocument.RootElement.GetProperty("roles").EnumerateArray()
            .Should().Contain(role => role.GetProperty("name").GetString() == "Usuario");
        using (var tokenScope = _fixture.Factory.Services.CreateScope())
        {
            var context = tokenScope.ServiceProvider.GetRequiredService<AppDbContext>();
            var revokedToken = await context.RefreshTokens
                .SingleAsync(token => token.Id == refreshTokenId);
            revokedToken.RevokedAt.Should().NotBeNull();
            revokedToken.RevokedReason.Should().Be("role_reassigned");
        }
    }

    [Fact]
    public async Task Admin_CannotRenameOrDeleteSystemRoles()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var soporteRoleId = await FindRoleIdAsync("Soporte");

        using var update = await client.PutAsync(
            $"/api/v1/roles/{soporteRoleId}",
            JsonContent(new { name = "SupportRenamed", description = "Not allowed" }));
        using var delete = await client.DeleteAsync($"/api/v1/roles/{soporteRoleId}");

        update.StatusCode.Should().Be(HttpStatusCode.Conflict);
        delete.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var updateDescription = await client.PutAsync(
            $"/api/v1/roles/{soporteRoleId}",
            JsonContent(new { name = "Soporte", description = "Descripción actualizada" }));
        updateDescription.StatusCode.Should().Be(HttpStatusCode.OK, await updateDescription.Content.ReadAsStringAsync());
        using var updatedRole = JsonDocument.Parse(await updateDescription.Content.ReadAsStringAsync());
        updatedRole.RootElement.GetProperty("name").GetString().Should().Be("Soporte");
        updatedRole.RootElement.GetProperty("description").GetString().Should().Be("Descripción actualizada");

        var adminRoleId = await FindRoleIdAsync("Admin");
        using var updateAdminDescription = await client.PutAsync(
            $"/api/v1/roles/{adminRoleId}",
            JsonContent(new { name = "Admin", description = "Descripción de Admin actualizada" }));
        updateAdminDescription.StatusCode.Should().Be(HttpStatusCode.OK, await updateAdminDescription.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Admin_CanReadAndUpdateRoleAndNamesAreCaseInsensitiveUnique()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;
        var roleName = $"Role-{Guid.NewGuid():N}";
        var roleId = Guid.Empty;

        using (var create = await client.PostAsync(
            "/api/v1/roles",
            JsonContent(new { name = roleName, description = "Before update", copyPermissionsFromRoleId = (Guid?)null })))
        {
            create.StatusCode.Should().Be(HttpStatusCode.Created, await create.Content.ReadAsStringAsync());
            using var created = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
            roleId = created.RootElement.GetProperty("id").GetGuid();
        }

        using (var list = await client.GetAsync("/api/v1/roles"))
        using (var roles = JsonDocument.Parse(await list.Content.ReadAsStringAsync()))
        {
            list.StatusCode.Should().Be(HttpStatusCode.OK);
            roles.RootElement.EnumerateArray().Should().Contain(role =>
                role.GetProperty("id").GetGuid() == roleId &&
                role.GetProperty("name").GetString() == roleName &&
                !role.GetProperty("isSystem").GetBoolean());
        }

        using (var details = await client.GetAsync($"/api/v1/roles/{roleId}"))
        using (var role = JsonDocument.Parse(await details.Content.ReadAsStringAsync()))
        {
            details.StatusCode.Should().Be(HttpStatusCode.OK);
            role.RootElement.GetProperty("description").GetString().Should().Be("Before update");
        }

        using (var update = await client.PutAsync(
            $"/api/v1/roles/{roleId}",
            JsonContent(new { name = $"{roleName}-Renamed", description = "After update" })))
        {
            update.StatusCode.Should().Be(HttpStatusCode.OK, await update.Content.ReadAsStringAsync());
            using var updated = JsonDocument.Parse(await update.Content.ReadAsStringAsync());
            updated.RootElement.GetProperty("name").GetString().Should().Be($"{roleName}-Renamed");
        }

        using (var duplicate = await client.PostAsync(
            "/api/v1/roles",
            JsonContent(new { name = $"{roleName}-rEnAmEd", description = "Duplicate", copyPermissionsFromRoleId = (Guid?)null })))
        {
            duplicate.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
            using var problem = JsonDocument.Parse(await duplicate.Content.ReadAsStringAsync());
            problem.RootElement.GetProperty("code").GetString().Should().Be("role_name_taken");
        }

        using var delete = await client.DeleteAsync($"/api/v1/roles/{roleId}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Theory]
    [InlineData("Soporte")]
    [InlineData("Usuario")]
    public async Task NonAdmin_ReceivesForbiddenForEveryRoleEndpoint(string role)
    {
        using var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: role);
        await SetBearerTokenAsync(client, user.Email, user.Password);
        var roleId = await FindRoleIdAsync(role);

        var responses = new[]
        {
            await client.GetAsync("/api/v1/roles"),
            await client.GetAsync($"/api/v1/roles/{roleId}"),
            await client.PostAsync("/api/v1/roles", JsonContent(new { name = "DeniedRole", description = "", copyPermissionsFromRoleId = (Guid?)null })),
            await client.PutAsync($"/api/v1/roles/{roleId}", JsonContent(new { name = role, description = "" })),
            await client.DeleteAsync($"/api/v1/roles/{roleId}")
        };

        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.Dispose();
        }
    }

    [Fact]
    public async Task CreatingAdminRoleNameIsRejected()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;

        using var response = await client.PostAsync(
            "/api/v1/roles",
            JsonContent(new { name = "aDmIn", description = "Impersonator", copyPermissionsFromRoleId = (Guid?)null }));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        problem.RootElement.GetProperty("code").GetString().Should().Be("role_name_invalid");
    }

    [Fact]
    public async Task CreatingRoleWithoutCopyStartsWithDashboardAndProfilePermissions()
    {
        var admin = await CreateAdminClientAsync();
        using var client = admin.Client;

        using var response = await client.PostAsync(
            "/api/v1/roles",
            JsonContent(new { name = $"Default-{Guid.NewGuid():N}", description = (string?)null, copyPermissionsFromRoleId = (Guid?)null }));

        response.StatusCode.Should().Be(HttpStatusCode.Created, await response.Content.ReadAsStringAsync());
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var permissions = document.RootElement.GetProperty("permissions").EnumerateArray()
            .Select(permission => permission.GetString())
            .ToArray();
        permissions.Should().Contain(["dashboard.access", "profile.access", "profile.update"]);
        permissions.Should().HaveCount(3);
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

    private static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
