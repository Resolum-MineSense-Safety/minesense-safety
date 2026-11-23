using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace MineSenseSafety.AcceptanceTests.Support;

/// <summary>
/// Clients for the deployed environment. One gateway serves every microservice, so all of them share
/// the same base address. The access token comes from <c>ACCEPTANCE_TOKEN</c> or, when only
/// <c>ACCEPTANCE_USER</c>/<c>ACCEPTANCE_PASSWORD</c> are given, from a sign-in through Identity &amp;
/// Access that is renewed shortly before it expires (tokens last 15 minutes, ADR-007).
/// </summary>
public static class RemoteEnvironment
{
    private static readonly SemaphoreSlim SignInLock = new(1, 1);
    private static string? _signedInToken;
    private static DateTimeOffset _signedInTokenExpiresAt = DateTimeOffset.MinValue;

    private static readonly Lazy<HttpClient> SharedClient = new(() =>
        new HttpClient { BaseAddress = AcceptanceSettings.BaseUrl, Timeout = AcceptanceSettings.RemoteTimeout });

    /// <summary>A client that carries the acceptance token, if one is configured.</summary>
    public static async Task<HttpClient> AuthorizedClientAsync()
    {
        var token = await AccessTokenAsync();
        return token is null ? AnonymousClient() : WithBearer(token);
    }

    /// <summary>A client without credentials, for the security scenarios.</summary>
    public static HttpClient AnonymousClient() => SharedClient.Value;

    /// <summary>A client that presents the given bearer token (valid or not).</summary>
    public static HttpClient WithBearer(string token)
    {
        var client = new HttpClient(new HttpClientHandler(), disposeHandler: true)
        {
            BaseAddress = AcceptanceSettings.BaseUrl,
            Timeout = AcceptanceSettings.RemoteTimeout
        };
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string?> AccessTokenAsync()
    {
        if (AcceptanceSettings.Token is { } token) return token;
        if (AcceptanceSettings.User is null || AcceptanceSettings.Password is null) return null;

        await SignInLock.WaitAsync();
        try
        {
            if (_signedInToken is null || DateTimeOffset.UtcNow > _signedInTokenExpiresAt - TimeSpan.FromMinutes(2))
            {
                var response = await SharedClient.Value.PostAsJsonAsync("api/v1/authentication/sign-in",
                    new { username = AcceptanceSettings.User, password = AcceptanceSettings.Password });
                if (!response.IsSuccessStatusCode)
                    throw new InvalidOperationException(
                        $"The acceptance user could not sign in (HTTP {(int)response.StatusCode}).");
                var session = (await response.Content.ReadFromJsonAsync<SignInResult>())!;
                _signedInToken = session.AccessToken;
                _signedInTokenExpiresAt = session.AccessTokenExpiresAt;
            }

            return _signedInToken;
        }
        finally
        {
            SignInLock.Release();
        }
    }

    private sealed record SignInResult(string AccessToken, DateTimeOffset AccessTokenExpiresAt);
}
