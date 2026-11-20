using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using FluentAssertions;
using GelyxLabs.Infrastructure.Persistence;
using GelyxLabs.Application.Abstractions.Settings;
using Testcontainers.PostgreSql;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public class RefreshTokenTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public RefreshTokenTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task RefreshTokens_AreReturnedInPlaintextButOnlyHashesAreStored()
    {
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));
        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        using var loginDocument = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        var initialRefreshToken = loginDocument.RootElement.GetProperty("refreshToken").GetString()!;
        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken = initialRefreshToken }), Encoding.UTF8, "application/json"));
        await refreshResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        using var refreshDocument = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        var rotatedRefreshToken = refreshDocument.RootElement.GetProperty("refreshToken").GetString()!;

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userId = await context.Users
            .Where(candidate => candidate.Email == user.Email)
            .Select(candidate => candidate.Id)
            .SingleAsync();
        var storedTokenHashes = await context.RefreshTokens
            .IgnoreQueryFilters()
            .Where(token => token.UserId == userId)
            .Select(token => token.TokenHash)
            .ToListAsync();

        Assert.Equal(86, initialRefreshToken.Length);
        Assert.Equal(86, rotatedRefreshToken.Length);
        Assert.DoesNotContain(initialRefreshToken, storedTokenHashes);
        Assert.DoesNotContain(rotatedRefreshToken, storedTokenHashes);
    }

    [Fact]
    public async Task Refresh_ValidToken_RotatesAndOldTokenIsReplaced()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act - Login first
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);

        var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString()!;

        // Act - Refresh
        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        // Assert
        await refreshResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var refreshBody = await refreshResponse.Content.ReadAsStringAsync();
        var refreshDoc = JsonDocument.Parse(refreshBody);

        refreshDoc.RootElement.GetProperty("accessToken").Should().NotBeNull();
        refreshDoc.RootElement.GetProperty("refreshToken").Should().NotBeNull();

        // Old token should be invalidated
        var oldTokenRefreshResponse = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        await oldTokenRefreshResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_ReusedToken_Returns401_token_reused_AndRevokesChain()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act - Login first
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);

        var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString()!;

        // Act - First refresh (should succeed)
        var firstRefresh = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        await firstRefresh.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var firstBody = await firstRefresh.Content.ReadAsStringAsync();
        var firstDoc = JsonDocument.Parse(firstBody);
        var newRefreshToken = firstDoc.RootElement.GetProperty("refreshToken").GetString()!;

        // Act - Second refresh with the old token (reused)
        var secondRefresh = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        // Assert
        await secondRefresh.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);

        var secondBody = await secondRefresh.Content.ReadAsStringAsync();
        var secondDoc = JsonDocument.Parse(secondBody);
        secondDoc.RootElement.GetProperty("code").GetString().Should().Be("token_reused");

        // Reuse revokes the whole token chain, including the replacement token.
        var thirdRefresh = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken = newRefreshToken }), Encoding.UTF8, "application/json"));

        await thirdRefresh.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Refresh_RevokedByLogout_Returns401_token_invalid()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act - Login
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);

        var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString()!;

        // Act - Logout
        var logoutResponse = await client.PostAsync("/api/v1/auth/logout",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        await logoutResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.NoContent);

        // Act - Try to refresh with revoked token
        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        // Assert
        await refreshResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);

        var refreshBody = await refreshResponse.Content.ReadAsStringAsync();
        var refreshDoc = JsonDocument.Parse(refreshBody);
        refreshDoc.RootElement.GetProperty("code").GetString().Should().Be("token_invalid");
        refreshDoc.RootElement.GetProperty("title").GetString().Should().Contain("inválido");
    }

    [Fact]
    public async Task Refresh_ConcurrentRequestsSameToken_OnlyOneSucceeds()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act - Login
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);

        var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString()!;

        // Act - Send two concurrent refresh requests with the same token
        var tasks = new Task<HttpResponseMessage>[]
        {
            client.PostAsync("/api/v1/auth/refresh",
                new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json")),
            client.PostAsync("/api/v1/auth/refresh",
                new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"))
        };

        await Task.WhenAll(tasks);

        // Assert - Only one should succeed, the other should fail
        var successCount = tasks.Count(t => t.Result.StatusCode == HttpStatusCode.OK);
        var failureCount = tasks.Count(t => t.Result.StatusCode == HttpStatusCode.Unauthorized);

        successCount.Should().Be(1);
        failureCount.Should().Be(1);
    }

    [Fact]
    public async Task Refresh_DeletedUser_ReturnsToken_invalid()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act - Login
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);

        var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString()!;
        await TestUserFactory.SetDeletedAsync(_fixture.Factory, user.Email);

        // Act - Try to refresh
        var refreshResponse = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        await refreshResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);
        using var responseBody = JsonDocument.Parse(await refreshResponse.Content.ReadAsStringAsync());
        responseBody.RootElement.GetProperty("code").GetString().Should().Be("token_invalid");
    }

    [Fact]
    public async Task ChangePassword_RevokesOtherTokens_KeepsCurrent()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act - Login
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);

        var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString()!;
        var accessToken = loginDoc.RootElement.GetProperty("accessToken").GetString()!;

        // Act - Change password
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            accessToken);
        var changePasswordResponse = await client.PostAsync("/api/v1/auth/change-password",
            new StringContent(JsonSerializer.Serialize(new { currentPassword = user.Password, newPassword = "NewPassword123!", refreshToken }), Encoding.UTF8, "application/json"));

        await changePasswordResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.NoContent);

        // Act - The current refresh token remains valid after the password change.
        var currentRefreshResponse = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        await currentRefreshResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        // Act - New login should work with new password
        var newLoginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = "NewPassword123!" }), Encoding.UTF8, "application/json"));

        await newLoginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task ResetPassword_RevokesAllTokens()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act - Login
        var loginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = user.Password }), Encoding.UTF8, "application/json"));

        await loginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var loginBody = await loginResponse.Content.ReadAsStringAsync();
        var loginDoc = JsonDocument.Parse(loginBody);

        var refreshToken = loginDoc.RootElement.GetProperty("refreshToken").GetString()!;
        var resetToken = await TestUserFactory.GeneratePasswordResetTokenAsync(_fixture.Factory, user.Email);
        var encodedResetToken = Convert.ToBase64String(Encoding.UTF8.GetBytes(resetToken));

        // Act - Reset password
        var resetResponse = await client.PostAsync("/api/v1/auth/reset-password",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, token = encodedResetToken, newPassword = "NewPassword123!" }), Encoding.UTF8, "application/json"));

        await resetResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.NoContent);

        // Act - Old tokens should be invalidated
        var oldRefreshResponse = await client.PostAsync("/api/v1/auth/refresh",
            new StringContent(JsonSerializer.Serialize(new { refreshToken }), Encoding.UTF8, "application/json"));

        await oldRefreshResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);

        var newLoginResponse = await client.PostAsync("/api/v1/auth/login",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email, password = "NewPassword123!" }), Encoding.UTF8, "application/json"));

        await newLoginResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_WhenDisabled_Returns404_registration_disabled()
    {
        // Arrange
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var settings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();
            await settings.UpdateAsync("auth.allowSelfRegistration", "false", "boolean");
        }
        var client = _fixture.Factory.CreateClient();
        var email = $"disabled.{Guid.NewGuid():N}@example.com";

        // Act - Try to register when self-registration is disabled
        var registerResponse = await client.PostAsync("/api/v1/auth/register",
            new StringContent(JsonSerializer.Serialize(new { email, password = "Test12345678", fullName = "New User" }), Encoding.UTF8, "application/json"));

        // Assert
        await registerResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.NotFound);
        using var body = JsonDocument.Parse(await registerResponse.Content.ReadAsStringAsync());
        body.RootElement.GetProperty("code").GetString().Should().Be("registration_disabled");
    }

    [Fact]
    public async Task ForgotPassword_Returns202_ForExistingAndUnknownEmail()
    {
        // Arrange
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);

        // Act - Forgot password with existing email
        var existingEmailResponse = await client.PostAsync("/api/v1/auth/forgot-password",
            new StringContent(JsonSerializer.Serialize(new { email = user.Email }), Encoding.UTF8, "application/json"));

        await existingEmailResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.Accepted);

        // Act - Forgot password with unknown email
        var unknownEmailResponse = await client.PostAsync("/api/v1/auth/forgot-password",
            new StringContent(JsonSerializer.Serialize(new { email = "nonexistent@test.com" }), Encoding.UTF8, "application/json"));

        await unknownEmailResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.Accepted);
    }
}
