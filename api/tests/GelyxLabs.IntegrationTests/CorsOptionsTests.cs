using GelyxLabs.Api;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public sealed class CorsOptionsTests
{
    [Fact]
    public void FromConfiguration_ParsesScalarAndIndexedAllowedOrigins()
    {
        var scalarConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins"] = "https://app.example, https://admin.example"
            })
            .Build();
        var indexedConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Cors:AllowedOrigins:0"] = "https://app.example",
                ["Cors:AllowedOrigins:1"] = "https://admin.example"
            })
            .Build();

        CorsOptions.FromConfiguration(scalarConfiguration.GetSection("Cors"))
            .AllowedOrigins.Should().Equal("https://app.example", "https://admin.example");
        CorsOptions.FromConfiguration(indexedConfiguration.GetSection("Cors"))
            .AllowedOrigins.Should().Equal("https://app.example", "https://admin.example");
    }
}
