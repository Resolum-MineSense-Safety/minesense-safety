using Microsoft.AspNetCore.Mvc.Testing;

namespace AlertService.Tests.Interfaces;

/// <summary>
/// Every API response carries X-Content-Type-Options: nosniff, the header OWASP ZAP rule 10021
/// reported as missing (section 4.4.4), even when authentication rejects the request.
/// </summary>
public class SecurityHeadersTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public SecurityHeadersTests(WebApplicationFactory<Program> factory)
    {
        client = factory
            .WithWebHostBuilder(builder => builder.UseSetting("Authentication:IdentityUrl", "http://identity.invalid"))
            .CreateClient();
    }

    [Fact]
    public async Task ApiResponse_CarriesNoSniffHeader_EvenWhenUnauthorized()
    {
        // Act
        var response = await client.GetAsync("/api/v1/alerts/status/Issued");

        // Assert
        Assert.True(response.Headers.TryGetValues("X-Content-Type-Options", out var values));
        Assert.Equal("nosniff", Assert.Single(values));
    }
}
