using System.Net;
using System.Text;
using System.Text.Json;
using System.Net.Http.Headers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public class AuthFlowTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public AuthFlowTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Register_Then_Login_Me_Refresh_Logout_FullFlow()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();

        // Act - Register (with AllowSelfRegistration=true in fixture)
        var email = $"flowtest.{Guid.NewGuid():N}@test.com";
        var registerResponse = await client.PostAsync("/api/v1/auth/register",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Password123!", fullName = "Flow Test" }), Encoding.UTF8, "application/json"));

        await registerResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.Created);

        // Act - Login
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Password123!" }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);
        var accessToken = loginDoc.RootElement.GetProperty("accessToken").GetString()!;
        var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString()!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        // Act - Me
        var meResponse = await client.GetAsync("/api/v1/auth/me");
        await meResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        var meBody = await meResponse.Content.ReadAsStringAsync();
        var meDoc = JsonDocument.Parse(meBody);
        meDoc.RootElement.GetProperty("locale").GetString().Should().Be("es");
        meDoc.RootElement.GetProperty("theme").GetString().Should().Be("system");

        // Act - Refresh
        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        await refreshResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        var refreshedBody = await refreshResponse.Content.ReadAsStringAsync();
        var refreshedDoc = JsonDocument.Parse(refreshedBody);
        var newRefreshToken = refreshedDoc.RootElement.GetProperty("refreshToken").GetString()!;

        // Act - Logout
        var logoutResponse = await client.PostAsync("/api/v1/auth/logout",
            new StringContent(JsonSerializer.Serialize(new { refreshToken = newRefreshToken }), Encoding.UTF8, "application/json"));

        await logoutResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Preferences_UpdatePersistsLocaleAndTheme_AndRejectsUnsupportedValues()
    {
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);
        await SetBearerTokenAsync(client, user.Email, user.Password);

        var updateResponse = await client.PutAsync(
            "/api/v1/auth/me/preferences",
            new StringContent(JsonSerializer.Serialize(new { locale = "en", theme = "dark" }), Encoding.UTF8, "application/json"));
        await updateResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.NoContent);

        using var meResponse = await client.GetAsync("/api/v1/auth/me");
        await meResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        using var meDocument = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        meDocument.RootElement.GetProperty("locale").GetString().Should().Be("en");
        meDocument.RootElement.GetProperty("theme").GetString().Should().Be("dark");

        var invalidThemeResponse = await client.PutAsync(
            "/api/v1/auth/me/preferences",
            new StringContent(JsonSerializer.Serialize(new { locale = "en", theme = "midnight" }), Encoding.UTF8, "application/json"));
        await invalidThemeResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.UnprocessableEntity);
        using var invalidThemeDocument = JsonDocument.Parse(await invalidThemeResponse.Content.ReadAsStringAsync());
        invalidThemeDocument.RootElement.GetProperty("code").GetString().Should().Be("unsupported_theme");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act
        var response = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = "wrongpassword" }), Encoding.UTF8, "application/json"));

        // Assert
        await response.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401_WithCode_invalid_credentials()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var unknownEmail = $"unknown.{Guid.NewGuid():N}@example.com";

        // Act
        var response = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = unknownEmail, password = "Password123!" }), Encoding.UTF8, "application/json"));

        // Assert
        await response.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);
        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);
        document.RootElement.GetProperty("code").GetString().Should().Be("invalid_credentials");
    }

    [Fact]
    public async Task Login_InactiveUser_WithCorrectPassword_ReturnsCode_account_inactive()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, isActive: false);

        // Act
        var response = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        // Assert
        await response.ShouldHaveStatusCodeAsync(HttpStatusCode.Forbidden);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("code").GetString().Should().Be("account_inactive");
    }

    [Fact]
    public async Task Login_LockedOut_AfterFiveFailures_ReturnsCode_account_locked()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act - Hacer 5 intentos fallidos
        for (int i = 0; i < 5; i++)
        {
            await client.PostAsync("/api/v1/auth/login",
                new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = "wrongpassword" }), Encoding.UTF8, "application/json"));
        }

        // Act - Intento final
        var response = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        // Assert
        await response.ShouldHaveStatusCodeAsync(HttpStatusCode.Locked);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("code").GetString().Should().Be("account_locked");
    }

    [Fact]
    public async Task Me_AsSoporte_DoesNotContain_users_roles_permissions_audit_modules()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: "Soporte");

        // Act - Login as soporte
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginDoc.RootElement.GetProperty("accessToken").GetString());

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        await meResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var meBody = await meResponse.Content.ReadAsStringAsync();
        var meDoc = JsonDocument.Parse(meBody);

        // Assert - Soporte no debe tener users, roles, permissions, audit modules
        var modules = meDoc.RootElement.GetProperty("modules").EnumerateArray();
        foreach (var module in modules)
        {
            var key = module.GetProperty("key").GetString()!;
            foreach (var forbidden in new[] { "users", "roles", "permissions", "audit" })
            {
                key.Should().NotContain(forbidden);
            }
        }
        modules.Select(module => module.GetProperty("key").GetString())
            .Should().Contain(["dashboard", "profile"]);
    }

    [Fact]
    public async Task Profile_AsSoporte_CanUpdateOwnNameAndGetsAudited()
    {
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: "Soporte");
        await SetBearerTokenAsync(client, user.Email, user.Password);

        var updateResponse = await client.PutAsync(
            "/api/v1/profile/me",
            new StringContent(
                JsonSerializer.Serialize(new { fullName = "Updated Support Name" }),
                Encoding.UTF8,
                "application/json"));

        await updateResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.NoContent);
        var invalidUpdateResponse = await client.PutAsync(
            "/api/v1/profile/me",
            new StringContent(
                JsonSerializer.Serialize(new { fullName = "  " }),
                Encoding.UTF8,
                "application/json"));
        await invalidUpdateResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.UnprocessableEntity);

        using var meResponse = await client.GetAsync("/api/v1/auth/me");
        await meResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        using var meDocument = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        meDocument.RootElement.GetProperty("fullName").GetString().Should().Be("Updated Support Name");

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<GelyxLabs.Infrastructure.Persistence.AppDbContext>();
        var userId = await context.Users
            .Where(candidate => candidate.Email == user.Email)
            .Select(candidate => candidate.Id)
            .SingleAsync();
        var auditEntry = await context.AuditLogs
            .Where(log => log.Action == "profile.updated" && log.ActorUserId == userId)
            .OrderByDescending(log => log.CreatedAt)
            .FirstOrDefaultAsync();
        auditEntry.Should().NotBeNull();
    }

    [Fact]
    public async Task Profile_UpdateRequiresTheProfileUpdatePermission()
    {
        var role = $"ProfileReadOnly-{Guid.NewGuid():N}";
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role);
        await SetBearerTokenAsync(client, user.Email, user.Password);

        var response = await client.PutAsync(
            "/api/v1/profile/me",
            new StringContent(
                JsonSerializer.Serialize(new { fullName = "Updated Name" }),
                Encoding.UTF8,
                "application/json"));

        await response.ShouldHaveStatusCodeAsync(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Me_AsAdmin_ContainsAllActiveModules()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: "Admin");

        // Act - Login as admin
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            loginDoc.RootElement.GetProperty("accessToken").GetString());

        var meResponse = await client.GetAsync("/api/v1/auth/me");
        await meResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var meBody = await meResponse.Content.ReadAsStringAsync();
        var meDoc = JsonDocument.Parse(meBody);

        // Assert - Admin debe tener todos los módulos activos
        var modules = meDoc.RootElement.GetProperty("modules").EnumerateArray();
        var moduleKeys = modules.Select(m => m.GetProperty("key").GetString()).ToList();
        moduleKeys.Should().Contain("users").And.Contain("roles").And.Contain("permissions").And.Contain("audit");
    }

    private static async Task SetBearerTokenAsync(HttpClient client, string email, string password)
    {
        var loginResponse = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent(
                JsonSerializer.Serialize(new { email, password }),
                Encoding.UTF8,
                "application/json"));
        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        using var login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            login.RootElement.GetProperty("accessToken").GetString());
    }

    [Fact]
    public async Task UserLocale_DefaultsToSpanish_AndCanBeUpdatedToSupportedLocales()
    {
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);
        await SetBearerTokenAsync(client, user.Email, user.Password);

        using var initialResponse = await client.GetAsync("/api/v1/auth/me");
        await initialResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        using var initialDocument = JsonDocument.Parse(await initialResponse.Content.ReadAsStringAsync());
        initialDocument.RootElement.GetProperty("locale").GetString().Should().Be("es");

        using var updateResponse = await client.PutAsync(
            "/api/v1/auth/me/preferences",
            new StringContent(JsonSerializer.Serialize(new { locale = "en" }), Encoding.UTF8, "application/json"));
        await updateResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.NoContent);

        using var updatedResponse = await client.GetAsync("/api/v1/auth/me");
        using var updatedDocument = JsonDocument.Parse(await updatedResponse.Content.ReadAsStringAsync());
        updatedDocument.RootElement.GetProperty("locale").GetString().Should().Be("en");

        using var invalidResponse = await client.PutAsync(
            "/api/v1/auth/me/preferences",
            new StringContent(JsonSerializer.Serialize(new { locale = "fr" }), Encoding.UTF8, "application/json"));
        await invalidResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.UnprocessableEntity);
        using var invalidDocument = JsonDocument.Parse(await invalidResponse.Content.ReadAsStringAsync());
        invalidDocument.RootElement.GetProperty("code").GetString().Should().Be("unsupported_locale");
    }

}
