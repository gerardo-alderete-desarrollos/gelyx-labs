using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GelyxLabs.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public class SessionsEndpointsTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public SessionsEndpointsTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Sessions_ListMarksCurrentSessionAndDoesNotExposeTokenHashes()
    {
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);
        var firstLogin = await LoginAsync(client, user.Email, user.Password);
        var secondLogin = await LoginAsync(client, user.Email, user.Password);
        SetSessionHeaders(client, firstLogin.AccessToken, firstLogin.RefreshToken);

        using var response = await client.GetAsync("/api/v1/auth/sessions");

        await response.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var sessions = document.RootElement.EnumerateArray().ToArray();
        sessions.Should().HaveCount(2);
        sessions.Should().ContainSingle(session => session.GetProperty("current").GetBoolean());
        foreach (var session in sessions)
        {
            session.TryGetProperty("tokenHash", out var tokenHash).Should().BeFalse();
            session.TryGetProperty("agent", out var agent).Should().BeTrue();
            session.TryGetProperty("createdByIp", out var createdByIp).Should().BeTrue();
            session.TryGetProperty("createdAt", out var createdAt).Should().BeTrue();
            session.TryGetProperty("expiresAt", out var expiresAt).Should().BeTrue();
        }
        secondLogin.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Sessions_UserCannotRevokeAnotherUsersSession()
    {
        var ownerClient = _fixture.Factory.CreateClient();
        var otherClient = _fixture.Factory.CreateClient();
        var owner = await TestUserFactory.CreateAsync(_fixture.Factory);
        var other = await TestUserFactory.CreateAsync(_fixture.Factory);
        var ownerLogin = await LoginAsync(ownerClient, owner.Email, owner.Password);
        var otherLogin = await LoginAsync(otherClient, other.Email, other.Password);
        SetSessionHeaders(ownerClient, ownerLogin.AccessToken, ownerLogin.RefreshToken);

        Guid otherSessionId;
        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            otherSessionId = await context.RefreshTokens
                .Where(token => token.User.Email == other.Email && token.RevokedAt == null)
                .Select(token => token.Id)
                .SingleAsync();
        }

        using var response = await ownerClient.DeleteAsync($"/api/v1/auth/sessions/{otherSessionId}");

        await response.ShouldHaveStatusCodeAsync(HttpStatusCode.NotFound);
        otherLogin.RefreshToken.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Sessions_RevokeOthersInvalidatesOtherRefreshTokensAndAuditsEachRevocation()
    {
        var currentClient = _fixture.Factory.CreateClient();
        var otherClient = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory);
        var currentLogin = await LoginAsync(currentClient, user.Email, user.Password);
        var otherLogin = await LoginAsync(otherClient, user.Email, user.Password);
        SetSessionHeaders(currentClient, currentLogin.AccessToken, currentLogin.RefreshToken);

        using var revokeResponse = await currentClient.PostAsync("/api/v1/auth/sessions/revoke-others", null);

        await revokeResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        using (var document = JsonDocument.Parse(await revokeResponse.Content.ReadAsStringAsync()))
        {
            document.RootElement.GetProperty("revokedCount").GetInt32().Should().Be(1);
        }

        using var refreshResponse = await otherClient.PostAsync(
            "/api/v1/auth/refresh",
            new StringContent(
                JsonSerializer.Serialize(new { refreshToken = otherLogin.RefreshToken }),
                Encoding.UTF8,
                "application/json"));
        await refreshResponse.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);

        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var auditEntries = await context.AuditLogs
            .Where(log => log.Action == "session.revoked")
            .ToListAsync();
        auditEntries.Should().Contain(entry => entry.ActorUserId != null);
    }

    private static void SetSessionHeaders(HttpClient client, string accessToken, string refreshToken)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        client.DefaultRequestHeaders.Remove("X-Refresh-Token");
        client.DefaultRequestHeaders.Add("X-Refresh-Token", refreshToken);
    }

    private static async Task<LoginResponse> LoginAsync(HttpClient client, string email, string password)
    {
        using var response = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent(
                JsonSerializer.Serialize(new { email, password }),
                Encoding.UTF8,
                "application/json"));
        await response.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return new LoginResponse(
            document.RootElement.GetProperty("accessToken").GetString()!,
            document.RootElement.GetProperty("refreshToken").GetString()!);
    }

    private sealed record LoginResponse(string AccessToken, string RefreshToken);
}
