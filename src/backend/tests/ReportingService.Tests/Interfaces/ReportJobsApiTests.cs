using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
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

    private static object Request(string format, Guid? requestedBy, string definitionCode = "jobs-e2e") => new
    {
        from = "2026-10-01T00:00:00Z",
        to = "2026-10-14T00:00:00Z",
        siteId = "site-north",
        fleetId = "fleet-a",
        definitionCode,
        format,
        requestedBy
    };

    [Fact]
    public async Task PostSafetyReport_ReturnsCreatedReadyWithRequester()
    {
        // Arrange
        await SeedDefinition();
        var requestedBy = Guid.NewGuid();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/reports/safety", Request("Pdf", requestedBy));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var resource = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(resource);
        Assert.Equal("Ready", resource["status"]?.ToString());
        Assert.Equal(requestedBy.ToString(), resource["requestedBy"]?.ToString());
        Assert.Equal("v1.0", resource["definitionVersion"]?.ToString());
        Assert.Equal($"/api/v1/reports/{resource["id"]}/download", resource["downloadUri"]?.ToString());
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task PostSafetyReport_WithoutRequester_ReturnsUnprocessableEntity()
    {
        // Arrange
        await SeedDefinition();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/reports/safety", Request("Pdf", null));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PostSafetyReport_UnknownDefinition_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await client.PostAsJsonAsync("/api/v1/reports/safety",
            Request("Pdf", Guid.NewGuid(), "no-such-indicator"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PostSafetyReport_UnknownFormat_ReturnsUnprocessableEntity()
    {
        // Arrange
        await SeedDefinition();

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/reports/safety", Request("Docx", Guid.NewGuid()));

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

    [Theory]
    [InlineData("Pdf", "application/pdf", "%PDF-")]
    [InlineData("Csv", "text/csv", "field,value")]
    public async Task DownloadReport_ReadyJob_ReturnsFileWithChecksum(string format, string contentType, string prefix)
    {
        // Arrange
        await SeedDefinition();
        var created = await client.PostAsJsonAsync("/api/v1/reports/safety", Request(format, Guid.NewGuid()));
        var resource = await created.Content.ReadFromJsonAsync<Dictionary<string, object>>();

        // Act
        var download = await client.GetAsync($"/api/v1/reports/{resource!["id"]}/download");
        var bytes = await download.Content.ReadAsByteArrayAsync();

        // Assert
        Assert.Equal(HttpStatusCode.OK, download.StatusCode);
        Assert.Equal(contentType, download.Content.Headers.ContentType?.MediaType);
        Assert.StartsWith(prefix, System.Text.Encoding.UTF8.GetString(bytes));
        Assert.Equal("sha256:" + Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant(),
            download.Headers.GetValues("X-Checksum").Single());
    }

    [Fact]
    public async Task DownloadReport_UnknownJob_ReturnsNotFound()
    {
        // Act
        var download = await client.GetAsync($"/api/v1/reports/{Guid.NewGuid()}/download");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, download.StatusCode);
    }

    [Fact]
    public async Task GetReportJobs_FilteredAndPaginated()
    {
        // Arrange
        await SeedDefinition();
        await client.PostAsJsonAsync("/api/v1/reports/safety", Request("Csv", Guid.NewGuid()));

        // Act
        var list = await client.GetAsync("/api/v1/reports?siteId=site-north&page=1&pageSize=20");
        var badPage = await client.GetAsync("/api/v1/reports?page=0");

        // Assert
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Contains("jobs-e2e", await list.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.UnprocessableEntity, badPage.StatusCode);
    }
}
