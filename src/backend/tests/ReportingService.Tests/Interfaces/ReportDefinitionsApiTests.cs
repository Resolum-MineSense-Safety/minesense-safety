using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ReportingService.Tests.Interfaces;

public class ReportDefinitionsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ReportDefinitionsApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task PostDefinition_ReturnsCreated()
    {
        // Arrange
        var body = new
        {
            code = "fatigue-by-shift",
            name = "Fatigue alerts by shift",
            version = "v1.0",
            dataSources = new[] { "fatigue-assessments", "alerts" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/report-definitions", body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var resource = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(resource);
        Assert.Equal("fatigue-by-shift", resource["code"]?.ToString());
    }

    [Fact]
    public async Task PostDefinition_DuplicateCodeAndVersion_ReturnsConflict()
    {
        // Arrange
        var body = new
        {
            code = "incidents-by-fleet",
            name = "Incidents by fleet",
            version = "v1.0",
            dataSources = new[] { "incidents" }
        };
        await client.PostAsJsonAsync("/api/v1/report-definitions", body);

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/report-definitions", body);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task PostDefinition_BlankCode_ReturnsUnprocessableEntity()
    {
        // Arrange
        var body = new
        {
            code = "  ",
            name = "No code",
            version = "v1.0",
            dataSources = new[] { "alerts" }
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/report-definitions", body);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetDefinitions_ReturnsCreatedOnes()
    {
        // Arrange
        await client.PostAsJsonAsync("/api/v1/report-definitions", new
        {
            code = "alerts-by-turn",
            name = "Alerts by turn",
            version = "v1.0",
            dataSources = new[] { "alerts" }
        });

        // Act
        var response = await client.GetAsync("/api/v1/report-definitions?onlyActive=true");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("alerts-by-turn", body);
    }
}
