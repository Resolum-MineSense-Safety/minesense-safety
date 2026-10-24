using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OperationsService.Tests.Interfaces;

public class HealthChecksApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Theory]
    [InlineData("/health/live")]
    [InlineData("/health/ready")]
    public async Task GetProbe_WithoutToken_ReturnsHealthy(string path)
    {
        // Act
        var response = await _client.GetAsync(path);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync());
    }
}
