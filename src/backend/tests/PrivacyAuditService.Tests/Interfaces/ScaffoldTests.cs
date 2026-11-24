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
    [InlineData("/api/v1/consents")]
    [InlineData("/api/v1/access-log/entries")]
    [InlineData("/api/v1/audit/compliance-reports")]
    public async Task ScaffoldPostRoutes_WithoutBody_ReturnBadRequest(string url)
    {
        // Arrange & Act: empty body fails [ApiController] validation before reaching the 501 stub
        var response = await client.PostAsync(url, new StringContent("{}", System.Text.Encoding.UTF8, "application/json"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
