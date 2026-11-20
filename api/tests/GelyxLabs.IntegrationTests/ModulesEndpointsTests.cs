using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GelyxLabs.Domain.Entities;
using GelyxLabs.Infrastructure.Persistence;
using GelyxLabs.Infrastructure.Services;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public class ModulesEndpointsTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public ModulesEndpointsTests(DatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Admin_CanUpdateModuleAndMeUsesLocalizedName()
    {
        using var client = await CreateClientAsync("Admin");
        var module = await FindModuleAsync("dashboard");
        var request = new
        {
            translations = new
            {
                es = new { name = "Inicio personalizado", description = "Panel personalizado" },
                en = new { name = "Custom home", description = "Custom dashboard" }
            },
            icon = "layout-dashboard",
            sortOrder = 7
        };

        using var updateResponse = await client.PutAsync(
            $"/api/v1/modules/{module.Id}",
            JsonContent(request));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK, await updateResponse.Content.ReadAsStringAsync());
        using var update = JsonDocument.Parse(await updateResponse.Content.ReadAsStringAsync());
        update.RootElement.GetProperty("sortOrder").GetInt32().Should().Be(7);
        update.RootElement.GetProperty("translations").GetProperty("es").GetProperty("name").GetString()
            .Should().Be("Inicio personalizado");
        update.RootElement.GetProperty("permissions").GetArrayLength().Should().BeGreaterThan(0);

        using var meResponse = await client.GetAsync("/api/v1/auth/me");
        meResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var me = JsonDocument.Parse(await meResponse.Content.ReadAsStringAsync());
        me.RootElement.GetProperty("modules").EnumerateArray()
            .Single(item => item.GetProperty("key").GetString() == "dashboard")
            .GetProperty("name").GetString().Should().Be("Inicio personalizado");
    }

    [Fact]
    public async Task Resynchronizing_DoesNotRevertModuleVisualEdits()
    {
        using var client = await CreateClientAsync("Admin");
        var module = await FindModuleAsync("dashboard");
        var request = new
        {
            translations = new
            {
                es = new { name = "Inicio persistente", description = "Panel persistente" },
                en = new { name = "Persistent home", description = "Persistent dashboard" }
            },
            icon = "layout-dashboard",
            sortOrder = 12
        };
        using var updateResponse = await client.PutAsync($"/api/v1/modules/{module.Id}", JsonContent(request));
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using (var scope = _fixture.Factory.Services.CreateScope())
        {
            var synchronizer = scope.ServiceProvider.GetRequiredService<IModuleSynchronizer>();
            await synchronizer.SynchronizeAsync();
        }

        using var getResponse = await client.GetAsync($"/api/v1/modules/{module.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var result = JsonDocument.Parse(await getResponse.Content.ReadAsStringAsync());
        result.RootElement.GetProperty("sortOrder").GetInt32().Should().Be(12);
        result.RootElement.GetProperty("translations").GetProperty("es").GetProperty("name").GetString()
            .Should().Be("Inicio persistente");
    }

    [Fact]
    public async Task DisablingUsersModule_Returns422()
    {
        using var client = await CreateClientAsync("Admin");
        var usersModule = await FindModuleAsync("users");

        using var response = await client.PatchAsync(
            $"/api/v1/modules/{usersModule.Id}/status",
            JsonContent(new { isActive = false }));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        await AssertCodeAsync(response, "module_status_protected");
    }

    [Fact]
    public async Task DisablingNonSystemModule_RemovesItFromMe()
    {
        var module = await CreateNonSystemModuleAsync();
        using var client = await CreateClientAsync("Admin");

        using (var beforeResponse = await client.GetAsync("/api/v1/auth/me"))
        using (var before = JsonDocument.Parse(await beforeResponse.Content.ReadAsStringAsync()))
        {
            before.RootElement.GetProperty("modules").EnumerateArray()
                .Select(item => item.GetProperty("key").GetString())
                .Should().Contain(module.Key);
        }

        using var statusResponse = await client.PatchAsync(
            $"/api/v1/modules/{module.Id}/status",
            JsonContent(new { isActive = false }));
        statusResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var response = await client.GetAsync("/api/v1/auth/me");
        using var me = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        me.RootElement.GetProperty("modules").EnumerateArray()
            .Select(item => item.GetProperty("key").GetString())
            .Should().NotContain(module.Key);
    }

    [Theory]
    [InlineData("Soporte")]
    [InlineData("Usuario")]
    public async Task NonAdmins_AreForbiddenFromEveryModuleOperation(string role)
    {
        using var client = await CreateClientAsync(role);
        var module = await FindModuleAsync("dashboard");
        var update = new
        {
            translations = new
            {
                es = new { name = "Panel", description = "Panel" },
                en = new { name = "Dashboard", description = "Dashboard" }
            },
            icon = "layout-dashboard",
            sortOrder = 1
        };

        var responses = new[]
        {
            await client.GetAsync("/api/v1/modules"),
            await client.GetAsync($"/api/v1/modules/{module.Id}"),
            await client.PutAsync($"/api/v1/modules/{module.Id}", JsonContent(update)),
            await client.PatchAsync($"/api/v1/modules/{module.Id}/status", JsonContent(new { isActive = true }))
        };
        foreach (var response in responses)
        {
            response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
            response.Dispose();
        }
    }

    private async Task<(Guid Id, string Key)> FindModuleAsync(string key)
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        return await context.Modules
            .Where(module => module.Key == key)
            .Select(module => new ValueTuple<Guid, string>(module.Id, module.Key))
            .SingleAsync();
    }

    private async Task<(Guid Id, string Key)> CreateNonSystemModuleAsync()
    {
        using var scope = _fixture.Factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var key = $"custom-{Guid.NewGuid():N}";
        var module = new AppModule
        {
            Key = key,
            Name = "Custom",
            Description = "Custom module",
            Translations = new Dictionary<string, ModuleTranslation>
            {
                ["es"] = new() { Name = "Personalizado", Description = "Módulo personalizado" },
                ["en"] = new() { Name = "Custom", Description = "Custom module" }
            },
            Icon = "box",
            Route = "/dashboard/custom",
            SortOrder = 90,
            IsActive = true
        };
        context.Modules.Add(module);
        await context.SaveChangesAsync();
        return (module.Id, module.Key);
    }

    private async Task<HttpClient> CreateClientAsync(string role)
    {
        var client = _fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(_fixture.Factory, role: role);
        using var loginResponse = await client.PostAsync(
            "/api/v1/auth/login",
            new StringContent(
                JsonSerializer.Serialize(new { email = user.Email, password = user.Password }),
                Encoding.UTF8,
                "application/json"));
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        using var login = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            login.RootElement.GetProperty("accessToken").GetString());
        return client;
    }

    private static StringContent JsonContent<T>(T value) =>
        new(JsonSerializer.Serialize(value), Encoding.UTF8, "application/json");

    private static async Task AssertCodeAsync(HttpResponseMessage response, string expectedCode)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        document.RootElement.GetProperty("code").GetString().Should().Be(expectedCode);
    }
}
