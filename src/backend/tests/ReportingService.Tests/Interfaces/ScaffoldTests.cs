using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace ReportingService.Tests.Interfaces;

public class ScaffoldTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public ScaffoldTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Fact]
    public async Task SwaggerDefinition_IsAvailable()
    {
        // Arrange & Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/reports?siteId=site-north&page=1&pageSize=20")]
    [InlineData("/api/v1/reports/7c9e6679-7425-40de-944b-e07fc1f90ae7")]
    [InlineData("/api/v1/reports/7c9e6679-7425-40de-944b-e07fc1f90ae7/download")]
    public async Task ScaffoldGetRoutes_ReturnNotImplemented(string url)
    {
        // Arrange & Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/report-definitions")]
    [InlineData("/api/v1/reports/safety")]
    public async Task ScaffoldPostRoutes_WithoutBody_ReturnBadRequest(string url)
    {
        // Arrange & Act: empty body fails [ApiController] validation before reaching the 501 stub
        var response = await client.PostAsync(url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
