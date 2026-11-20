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

public class PermissionAuthorizationTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public PermissionAuthorizationTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task PermissionEndpoint_AdminIsAllowed()
    {
        using var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: "Admin");
        await SetBearerTokenAsync(client, user.Email, user.Password);

        var response = await client.GetAsync("/api/v1/permission-test");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task PermissionEndpoint_SoporteIsForbidden()
    {
        using var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: "Soporte");
        await SetBearerTokenAsync(client, user.Email, user.Password);

        var response = await client.GetAsync("/api/v1/permission-test");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task PermissionEndpoint_AnonymousIsUnauthorized()
    {
        using var client = _fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/v1/permission-test");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PermissionEndpoint_GrantingRolePermissionAndInvalidatingCacheAllowsAccess()
    {
        var roleName = $"PermissionTest-{Guid.NewGuid():N}";
        using var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: roleName);
        await SetBearerTokenAsync(client, user.Email, user.Password);

        var deniedResponse = await client.GetAsync("/api/v1/permission-test");
        deniedResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var roleId = await context.Roles
                .Where(role => role.Name == roleName)
                .Select(role => role.Id)
                .SingleAsync();
            var permissionId = await context.Permissions
                .Where(permission => permission.Key == "users.read")
                .Select(permission => permission.Id)
                .SingleAsync();

            context.RolePermissions.Add(new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId
            });
            await context.SaveChangesAsync();
            scope.ServiceProvider.GetRequiredService<GelyxLabs.Application.Abstractions.Security.IPermissionService>()
                .InvalidateAll();
        }

        var allowedResponse = await client.GetAsync("/api/v1/permission-test");
        allowedResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task SetBearerTokenAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent(
                JsonSerializer.Serialize(new { email, password }),
                Encoding.UTF8,
                "application/json"));
        response.StatusCode.Should().Be(HttpStatusCode.OK, await response.Content.ReadAsStringAsync());

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accessToken = document.RootElement.GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
    }
}
