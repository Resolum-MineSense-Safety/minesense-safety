using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrivacyAuditService.Tests.Interfaces;

public class ReportsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ReportsApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task PostSafetyReport_ReturnsCreatedRequested()
    {
        // Arrange
        var body = new
        {
            from = "2026-10-01T00:00:00Z",
            to = "2026-10-14T00:00:00Z",
            siteId = "site-north",
            fleetId = "fleet-a",
            requestedBy = Guid.NewGuid()
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/reports/safety", body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var resource = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(resource);
        Assert.Equal("Requested", resource["status"]?.ToString());
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task PostSafetyReport_InvertedPeriod_ReturnsUnprocessableEntity()
    {
        // Arrange
        var body = new
        {
            from = "2026-10-14T00:00:00Z",
            to = "2026-10-01T00:00:00Z",
            siteId = "site-north",
            fleetId = (string?)null
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/reports/safety", body);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetReportById_UnknownId_ReturnsNotFound()
    {
        // Arrange & Act
        var response = await client.GetAsync($"/api/v1/reports/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReportById_CreatedReport_ReturnsOk()
    {
        // Arrange
        var created = await client.PostAsJsonAsync("/api/v1/reports/safety", new
        {
            from = "2026-10-01T00:00:00Z",
            to = "2026-10-02T00:00:00Z",
            siteId = (string?)null,
            fleetId = (string?)null
        });
        var resource = await created.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        // Act
        var response = await client.GetAsync($"/api/v1/reports/{resource!["id"]}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
