using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrivacyAuditService.Tests.Interfaces;

public class IncidentHistoryApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public IncidentHistoryApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task GetIncidentHistory_WithRecordedSteps_ReturnsChronology()
    {
        // Arrange
        var incidentId = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/v1/access-log/entries", new
        {
            userId = Guid.NewGuid(),
            resourceType = "Incident",
            resourceId = incidentId,
            fields = new[] { "status" },
            purpose = "incident-review",
            decision = "Granted"
        });
        await client.PostAsJsonAsync("/api/v1/access-log/entries", new
        {
            userId = Guid.NewGuid(),
            resourceType = "Incident",
            resourceId = incidentId,
            fields = new[] { "assignee" },
            purpose = "incident-assignment",
            decision = "Denied"
        });

        // Act
        var response = await client.GetAsync($"/api/v1/incidents/{incidentId}/history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("AccessGranted", body);
        Assert.Contains("AccessDenied", body);
    }

    [Fact]
    public async Task GetIncidentHistory_WithoutSteps_ReturnsEmptyArray()
    {
        // Arrange & Act
        var response = await client.GetAsync($"/api/v1/incidents/{Guid.NewGuid()}/history");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }
}
