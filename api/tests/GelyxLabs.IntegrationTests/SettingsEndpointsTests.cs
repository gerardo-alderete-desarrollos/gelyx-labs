using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public sealed class SettingsEndpointsTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public SettingsEndpointsTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Soporte_IsForbiddenFromSettings()
    {
        using var client = await CreateAuthenticatedClientAsync("Soporte");
        using var response = await client.GetAsync("/api/v1/settings");
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task EnablingSelfRegistration_AppliesWithoutRestart()
    {
        using var admin = await CreateAuthenticatedClientAsync("Admin");
        using var disableResponse = await admin.PutAsync(
            "/api/v1/settings/auth.allowSelfRegistration",
            JsonContent(new { value = false }));
        disableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var beforeClient = _fixture.Factory.CreateClient();
        using var beforeResponse = await RegisterAsync(beforeClient);
        beforeResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);

        using var enableResponse = await admin.PutAsync(
            "/api/v1/settings/auth.allowSelfRegistration",
            JsonContent(new { value = true }));
        enableResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var afterClient = _fixture.Factory.CreateClient();
        using var afterResponse = await RegisterAsync(afterClient);
        afterResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task UpdatingSetting_WithWrongValueType_Returns422()
    {
        using var admin = await CreateAuthenticatedClientAsync("Admin");
        using var response = await admin.PutAsync(
            "/api/v1/settings/auth.allowSelfRegistration",
            JsonContent(new { value = "yes" }));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be("setting_type_invalid");
    }

    [Fact]
    public async Task PublicSettings_OnlyReturnsPublicKeys()
    {
        using var admin = await CreateAuthenticatedClientAsync("Admin");
        using var updateResponse = await admin.PutAsync(
            "/api/v1/settings/flags.internalBeta",
            JsonContent(new { value = true }));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var publicClient = _fixture.Factory.CreateClient();
        using var response = await publicClient.GetAsync("/api/v1/settings/public");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var keys = document.RootElement.EnumerateArray()
            .Select(setting => setting.GetProperty("key").GetString())
            .ToArray();

        keys.Should().BeEquivalentTo(["app.name", "app.defaultLocale", "auth.allowSelfRegistration"]);
        keys.Should().NotContain("flags.internalBeta");
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string role)
    {
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: role);
        using var loginResponse = await client.PostAsync("/api/v1/auth/login", JsonContent(new { email = user.Email, password = user.Password }));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.RootElement.GetProperty("accessToken").GetString());
        return client;
    }

    private static Task<HttpResponseMessage> RegisterAsync(HttpClient client) =>
        client.PostAsync("/api/v1/auth/register", JsonContent(new
        {
            email = $"settings.{Guid.NewGuid():N}@example.com",
            password = "Password123!",
            fullName = "Settings Test"
        }));

    private static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");
}
