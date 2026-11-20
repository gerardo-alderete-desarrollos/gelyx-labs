using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace GelyxLabs.IntegrationTests;

public sealed class TwoFactorEndpointsTests(DatabaseFixture fixture) : IClassFixture<DatabaseFixture>
{
    [Fact]
    public async Task TwoFactor_LoginRequiresSecondStep_RejectsBadCode_AndConsumesRecoveryCode()
    {
        var client = fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(fixture.Factory);
        var regularLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = user.Password });
        using var regularBody = JsonDocument.Parse(await regularLogin.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", regularBody.RootElement.GetProperty("accessToken").GetString());

        var setup = await client.PostAsync("/api/v1/auth/2fa/setup", null);
        await setup.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        using var setupBody = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
        var secret = setupBody.RootElement.GetProperty("secret").GetString()!;
        var enable = await client.PostAsJsonAsync("/api/v1/auth/2fa/enable", new { code = Totp(secret) });
        await enable.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);
        using var enableBody = JsonDocument.Parse(await enable.Content.ReadAsStringAsync());
        var recoveryCode = enableBody.RootElement.GetProperty("recoveryCodes")[0].GetString()!;

        client.DefaultRequestHeaders.Authorization = null;
        var login = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = user.Password });
        using var loginBody = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        loginBody.RootElement.GetProperty("twoFactorRequired").GetBoolean().Should().BeTrue();
        loginBody.RootElement.TryGetProperty("accessToken", out var accessToken).Should().BeTrue();
        accessToken.ValueKind.Should().Be(JsonValueKind.Null);
        var temporaryToken = loginBody.RootElement.GetProperty("twoFactorToken").GetString()!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", temporaryToken);
        var me = await client.GetAsync("/api/v1/auth/me");
        await me.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);
        client.DefaultRequestHeaders.Authorization = null;

        var badCode = await client.PostAsJsonAsync("/api/v1/auth/login/2fa", new { twoFactorToken = temporaryToken, code = "000000" });
        await badCode.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);
        var success = await client.PostAsJsonAsync("/api/v1/auth/login/2fa", new { twoFactorToken = temporaryToken, code = recoveryCode });
        await success.ShouldHaveStatusCodeAsync(HttpStatusCode.OK);

        var secondLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = user.Password });
        using var secondLoginBody = JsonDocument.Parse(await secondLogin.Content.ReadAsStringAsync());
        var secondTemporaryToken = secondLoginBody.RootElement.GetProperty("twoFactorToken").GetString()!;
        var reusedCode = await client.PostAsJsonAsync("/api/v1/auth/login/2fa", new { twoFactorToken = secondTemporaryToken, code = recoveryCode });
        await reusedCode.ShouldHaveStatusCodeAsync(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TwoFactor_RecoveryCodeCanOnlyBeConsumedOnceConcurrently()
    {
        var client = fixture.Factory.CreateClient();
        var user = await TestUserFactory.CreateAsync(fixture.Factory);
        var regularLogin = await client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = user.Password });
        using var regularBody = JsonDocument.Parse(await regularLogin.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", regularBody.RootElement.GetProperty("accessToken").GetString());

        using var setup = await client.PostAsync("/api/v1/auth/2fa/setup", null);
        using var setupBody = JsonDocument.Parse(await setup.Content.ReadAsStringAsync());
        var secret = setupBody.RootElement.GetProperty("secret").GetString()!;
        using var enable = await client.PostAsJsonAsync("/api/v1/auth/2fa/enable", new { code = Totp(secret) });
        using var enableBody = JsonDocument.Parse(await enable.Content.ReadAsStringAsync());
        var recoveryCode = enableBody.RootElement.GetProperty("recoveryCodes")[0].GetString()!;

        client.DefaultRequestHeaders.Authorization = null;
        var challenges = await Task.WhenAll(
            client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = user.Password }),
            client.PostAsJsonAsync("/api/v1/auth/login", new { email = user.Email, password = user.Password }));
        var temporaryTokens = await Task.WhenAll(challenges.Select(async response =>
        {
            using (response)
            {
            using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            return body.RootElement.GetProperty("twoFactorToken").GetString()!;
            }
        }));

        var attempts = await Task.WhenAll(temporaryTokens.Select(token =>
            client.PostAsJsonAsync("/api/v1/auth/login/2fa", new { twoFactorToken = token, code = recoveryCode })));

        attempts.Count(response => response.StatusCode == HttpStatusCode.OK).Should().Be(1);
        attempts.Count(response => response.StatusCode == HttpStatusCode.Unauthorized).Should().Be(1);
        foreach (var response in attempts) response.Dispose();
    }

    private static string Totp(string secret)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bits = 0; var value = 0; var bytes = new List<byte>();
        foreach (var character in secret.Replace(" ", string.Empty).ToUpperInvariant()) { value = (value << 5) | alphabet.IndexOf(character); bits += 5; if (bits >= 8) { bytes.Add((byte)(value >> (bits - 8))); bits -= 8; } }
        var counter = BitConverter.GetBytes(DateTimeOffset.UtcNow.ToUnixTimeSeconds() / 30); if (BitConverter.IsLittleEndian) Array.Reverse(counter);
        var hash = HMACSHA1.HashData(bytes.ToArray(), counter); var offset = hash[^1] & 0x0f;
        var code = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | hash[offset + 3];
        return (code % 1_000_000).ToString("D6");
    }
}
