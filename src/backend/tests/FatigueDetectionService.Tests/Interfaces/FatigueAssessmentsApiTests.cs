using System.Net;
using System.Net.Http.Json;
using FatigueDetectionService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FatigueDetectionService.Tests.Interfaces;

public class FatigueAssessmentsApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/api/v1/fatigue-assessments";
    private readonly HttpClient _client = factory.CreateClient();

    private static AssessFatigueResource Readings(Guid operatorId, double perclos = 0.55, double blink = 8, double hrv = 17) =>
        new(operatorId, Guid.NewGuid(), perclos, blink, hrv);

    [Fact]
    public async Task PostAssessment_WithMicrosleepReadings_ReturnsCreatedCriticalAssessment()
    {
        // Act
        var response = await _client.PostAsJsonAsync(Route, Readings(Guid.NewGuid()));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var assessment = await response.Content.ReadFromJsonAsync<FatigueAssessmentResource>();
        Assert.Equal("Critical", assessment!.RiskLevel);
    }

    [Fact]
    public async Task PostAssessment_WithInvalidSignals_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync(Route, Readings(Guid.NewGuid(), perclos: 1.7));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetById_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var created = await (await _client.PostAsJsonAsync(Route, Readings(Guid.NewGuid())))
            .Content.ReadFromJsonAsync<FatigueAssessmentResource>();

        // Act
        var found = await _client.GetAsync($"{Route}/{created!.Id}");
        var missing = await _client.GetAsync($"{Route}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task GetByOperatorAndLatest_ReturnHistoryAndNewestAssessment()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await _client.PostAsJsonAsync(Route, Readings(operatorId, perclos: 0.05, blink: 15, hrv: 50));
        await _client.PostAsJsonAsync(Route, Readings(operatorId));

        // Act
        var history = await _client.GetFromJsonAsync<List<FatigueAssessmentResource>>($"{Route}?operatorId={operatorId}");
        var latest = await _client.GetFromJsonAsync<FatigueAssessmentResource>($"{Route}/latest?operatorId={operatorId}");

        // Assert
        Assert.Equal(2, history!.Count);
        Assert.Equal(operatorId, latest!.OperatorId);
    }

    [Fact]
    public async Task GetBySession_ReturnsOnlyThatSessionWithAlertFlag()
    {
        // Arrange
        var sessionId = Guid.NewGuid();
        await _client.PostAsJsonAsync(Route, new AssessFatigueResource(Guid.NewGuid(), sessionId, 0.05, 15, 50));
        await _client.PostAsJsonAsync(Route, new AssessFatigueResource(Guid.NewGuid(), sessionId, 0.55, 8, 17));
        await _client.PostAsJsonAsync(Route, Readings(Guid.NewGuid()));

        // Act
        var assessments = await _client.GetFromJsonAsync<List<FatigueAssessmentResource>>($"{Route}/sessions/{sessionId}");

        // Assert
        Assert.Equal(2, assessments!.Count);
        Assert.False(assessments[0].RequiresAlert);
        Assert.True(assessments[1].RequiresAlert);
    }

    [Fact]
    public async Task GetLatest_ForOperatorWithoutAssessments_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"{Route}/latest?operatorId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
