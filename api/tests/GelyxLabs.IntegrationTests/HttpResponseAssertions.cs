using System.Net;
using FluentAssertions;

namespace GelyxLabs.IntegrationTests;

internal static class HttpResponseAssertions
{
    public static async Task ShouldHaveStatusCodeAsync(
        this HttpResponseMessage response,
        HttpStatusCode expectedStatusCode)
    {
        var body = await response.Content.ReadAsStringAsync();
        response.StatusCode.Should().Be(expectedStatusCode, "Response body: {0}", body);
    }
}
