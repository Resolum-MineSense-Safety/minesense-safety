using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ReportingService.Tests.Interfaces;

public class ReportJobsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ReportJobsApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    private async Task SeedDefinition()
    {
        // Arrange: the generation endpoint requires a known definition code
        await client.PostAsJsonAsync("/api/v1/report-definitions", new
        {
            code = "jobs-e2e",
            name = "Jobs e2e",
            version = "v1.0",
            dataSources = new[] { "alerts" }
        });
    }

    [Fact]
    public async Task PostSafetyReport_ReturnsCreatedRequested()
    {
        // Arrange
        await SeedDefinition();
        var body = new
        {
            from = "2026-10-01T00:00:00Z",
            to = "2026-10-14T00:00:00Z",
            siteId = "site-north",
            fleetId = "fleet-a",
            definitionCode = "jobs-e2e",
            format = "Pdf"
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
    public async Task PostSafetyReport_UnknownDefinition_ReturnsUnprocessableEntity()
    {
        // Arrange
        var body = new
        {
            from = "2026-10-01T00:00:00Z",
            to = "2026-10-14T00:00:00Z",
            siteId = "site-north",
            fleetId = "fleet-a",
            definitionCode = "no-such-indicator",
            format = "Pdf"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/reports/safety", body);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PostSafetyReport_UnknownFormat_ReturnsUnprocessableEntity()
    {
        // Arrange
        var body = new
        {
            from = "2026-10-01T00:00:00Z",
            to = "2026-10-14T00:00:00Z",
            siteId = "site-north",
            fleetId = "fleet-a",
            definitionCode = "jobs-e2e",
            format = "Docx"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/reports/safety", body);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetReportJob_UnknownId_ReturnsNotFound()
    {
        // Arrange & Act
        var response = await client.GetAsync($"/api/v1/reports/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetReportJobs_FilteredAndDownloadWithoutArtifact()
    {
        // Arrange
        await SeedDefinition();
        var created = await client.PostAsJsonAsync("/api/v1/reports/safety", new
        {
            from = "2026-10-01T00:00:00Z",
            to = "2026-10-02T00:00:00Z",
            siteId = "site-north",
            fleetId = "fleet-a",
            definitionCode = "jobs-e2e",
            format = "Csv"
        });
        var resource = await created.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        // Act
        var list = await client.GetAsync("/api/v1/reports?siteId=site-north&page=1&pageSize=20");
        var download = await client.GetAsync($"/api/v1/reports/{resource!["id"]}/download");
        var badPage = await client.GetAsync("/api/v1/reports?page=0");

        // Assert
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("jobs-e2e", await list.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode); // no artifact until Fase 4
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badPage.StatusCode);
    }
}
