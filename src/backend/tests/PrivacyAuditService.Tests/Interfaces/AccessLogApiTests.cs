using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace PrivacyAuditService.Tests.Interfaces;

public class AccessLogApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient client;

    public AccessLogApiTests(WebApplicationFactory<Program> factory)
    {
        client = factory.CreateClient();
    }

    [Theory]
    [InlineData("Granted")]
    [InlineData("Denied")]
    public async Task PostAccessAttempt_ReturnsCreatedWithHash(string decision)
    {
        // Arrange
        var body = new
        {
            userId = Guid.NewGuid(),
            resourceType = "FatigueAssessment",
            resourceId = Guid.NewGuid(),
            fields = new[] { "riskLevel" },
            purpose = "shift-supervision",
            decision = decision
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/access-log/entries", body);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var resource = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        Assert.NotNull(resource);
        Assert.Equal(decision, resource["decision"]?.ToString());
        Assert.False(string.IsNullOrWhiteSpace(resource["hash"]?.ToString()));
    }

    [Fact]
    public async Task PostAccessAttempt_UnknownDecision_ReturnsUnprocessableEntity()
    {
        // Arrange
        var body = new
        {
            userId = Guid.NewGuid(),
            resourceType = "FatigueAssessment",
            resourceId = Guid.NewGuid(),
            fields = new[] { "riskLevel" },
            purpose = "shift-supervision",
            decision = "Maybe"
        };

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/access-log/entries", body);

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetAccessLog_FilterByDecision_ReturnsMatches()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await client.PostAsJsonAsync("/api/v1/access-log/entries", new
        {
            userId = userId,
            resourceType = "Alert",
            resourceId = Guid.NewGuid(),
            fields = new[] { "status" },
            purpose = "incident-review",
            decision = "Denied"
        });

        // Act
        var response = await client.GetAsync(
            $"/api/v1/access-log?userId={userId}&decision=Denied");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Contains("Denied", body);
    }

    [Fact]
    public async Task GetAccessLog_InvalidPage_ReturnsUnprocessableEntity()
    {
        // Arrange & Act
        var response = await client.GetAsync("/api/v1/access-log?page=0&pageSize=200");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetAccessLog_NoMatches_ReturnsEmptyArray()
    {
        // Arrange & Act
        var response = await client.GetAsync(
            $"/api/v1/access-log?userId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("[]", await response.Content.ReadAsStringAsync());
    }
}
