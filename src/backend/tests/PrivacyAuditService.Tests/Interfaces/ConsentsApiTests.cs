using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrivacyAuditService.Tests.Interfaces;

public class ConsentsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;
    private readonly Guid operatorId = Guid.NewGuid();

    public ConsentsApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task PostConsent_ReturnsCreatedWithBlocksMonitoring()
    {
        // Arrange
        var body = new
        {
            operatorId = operatorId,
            policyVersion = "v1.0",
            purposes = new[] { "fatigue-monitoring", "safety-alerts" },
            status = "Accepted"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/consents", body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var resource = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(resource);
        Assert.Equal("Accepted", resource["status"]?.ToString());
        Assert.Equal("False", resource["blocksMonitoring"]?.ToString());
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task PostSameConsentTwice_ReturnsOkWithoutDuplicate()
    {
        // Arrange
        var body = new
        {
            operatorId = Guid.NewGuid(),
            policyVersion = "v1.0",
            purposes = new[] { "fatigue-monitoring" },
            status = "Declined"
        };

        // Act
        var first = await client.PostAsJsonAsync("/api/v1/consents", body);
        var second = await client.PostAsJsonAsync("/api/v1/consents", body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
    }

    [Fact]
    public async Task PostConsent_UnknownStatus_ReturnsUnprocessableEntity()
    {
        // Arrange
        var body = new
        {
            operatorId = Guid.NewGuid(),
            policyVersion = "v1.0",
            purposes = new[] { "fatigue-monitoring" },
            status = "Maybe"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/consents", body);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrentConsent_UnknownOperator_ReturnsNotFound()
    {
        // Arrange & Act
        var response = await client.GetAsync($"/api/v1/consents/operator/{Guid.NewGuid()}/current");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProcessingPurposes_ReturnsCurrentVersion()
    {
        // Arrange & Act
        var response = await client.GetAsync("/api/v1/processing-purposes");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("v1.0", body);
        Assert.Contains("fatigue-monitoring", body);
    }
}
