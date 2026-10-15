using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrivacyAuditService.Tests.Interfaces;

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
    [InlineData("/api/v1/processing-purposes")]
    [InlineData("/api/v1/consents/operator/3fa85f64-5717-4562-b3fc-2c963f66afa6/current")]
    [InlineData("/api/v1/consents/operator/3fa85f64-5717-4562-b3fc-2c963f66afa6/history")]
    [InlineData("/api/v1/access-log?operatorId=3fa85f64-5717-4562-b3fc-2c963f66afa6")]
    [InlineData("/api/v1/incidents/7c9e6679-7425-40de-944b-e07fc1f90ae7/history")]
    public async Task ScaffoldGetRoutes_ReturnNotImplemented(string url)
    {
        // Arrange & Act
        var response = await client.GetAsync(url);

        // Assert
        Assert.Equal(HttpStatusCode.NotImplemented, response.StatusCode);
    }

    [Theory]
    [InlineData("/api/v1/consents")]
    [InlineData("/api/v1/access-log/entries")]
    [InlineData("/api/v1/reports/safety")]
    public async Task ScaffoldPostRoutes_WithoutBody_ReturnBadRequest(string url)
    {
        // Arrange & Act: empty body fails [ApiController] validation before reaching the 501 stub
        var response = await client.PostAsync(url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
