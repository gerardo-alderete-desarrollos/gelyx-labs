using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public sealed class CorrelationIdTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _fixture;

    public CorrelationIdTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task ProblemResponse_IncludesSuppliedCorrelationId()
    {
        using var client = _fixture.Factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/auth/login")
        {
            Content = JsonContent.Create(new { email = "invalid", password = "invalid" })
        };
        request.Headers.Add("X-Correlation-Id", "web-request-123");

        using var response = await client.SendAsync(request);
        var problem = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Headers.GetValues("X-Correlation-Id").Should().ContainSingle().Which.Should().Be("web-request-123");
        problem.Should().ContainKey("correlationId").WhoseValue.ToString().Should().Be("web-request-123");
    }

    [Fact]
    public async Task UnhandledException_HidesInternalDetailsAndIncludesCorrelationId()
    {
        using var client = _fixture.Factory.CreateClient();

        using var response = await client.GetAsync("/api/v1/exception-test");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        response.Headers.TryGetValues("X-Correlation-Id", out var values).Should().BeTrue();
        values.Should().ContainSingle();
        body.Should().Contain("\"correlationId\"");
        body.Should().NotContain("Sensitive test exception");
        body.Should().NotContain("InvalidOperationException");
        body.Should().NotContain("at GelyxLabs");
    }
}
