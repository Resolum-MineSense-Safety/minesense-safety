using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AlertService.Tests.Interfaces;

/// <summary>
/// Prometheus scrapes /metrics without a token (section 4.3), and the HTTP middleware records
/// every API request in http_request_duration_seconds, the source of the latency SLI.
/// </summary>
public class MetricsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public MetricsEndpointTests(WebApplicationFactory<Program> factory)
    {
        client = factory
            .WithWebHostBuilder(builder => builder.UseSetting("Authentication:IdentityUrl", "http://identity.invalid"))
            .CreateClient();
    }

    [Fact]
    public async Task Metrics_AnswersWithoutToken_InPrometheusFormat()
    {
        // Act
        var response = await client.GetAsync("/metrics");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.StartsWith("text/plain", response.Content.Headers.ContentType!.MediaType);
    }

    [Fact]
    public async Task Metrics_RecordApiRequestsWithStatusCode()
    {
        // Arrange: an API call rejected by authentication still counts for the error-rate SLI
        await client.GetAsync("/api/v1/alerts/status/Issued");

        // Act
        var body = await client.GetStringAsync("/metrics");

        // Assert
        Assert.Contains("http_request_duration_seconds", body);
        Assert.Contains("code=\"401\"", body);
    }
}
