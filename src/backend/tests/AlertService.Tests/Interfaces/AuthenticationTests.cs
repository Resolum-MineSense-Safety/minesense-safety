using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AlertService.Tests.Interfaces;

/// <summary>
/// With Authentication:IdentityUrl set (as in docker-compose and Helm) the REST API requires an
/// Identity &amp; Access token, while Swagger and the health probes stay public.
/// </summary>
public class AuthenticationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public AuthenticationTests(WebApplicationFactory<Program> factory)
    {
        client = factory
            .WithWebHostBuilder(builder => builder.UseSetting("Authentication:IdentityUrl", "http://identity.invalid"))
            .CreateClient();
    }

    [Fact]
    public async Task ApiWithoutToken_ReturnsUnauthorized()
    {
        var response = await client.GetAsync("/api/v1/alerts/status/Issued");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("/swagger/v1/swagger.json")]
    [InlineData("/health/live")]
    public async Task PublicRoutes_AnswerWithoutToken(string url)
    {
        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task ApiWithForgedToken_ReturnsUnauthorized()
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/alerts/status/Issued");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
            "eyJhbGciOiJSUzI1NiJ9.eyJzdWIiOiIxIn0.c2lnbmF0dXJl");

        var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
