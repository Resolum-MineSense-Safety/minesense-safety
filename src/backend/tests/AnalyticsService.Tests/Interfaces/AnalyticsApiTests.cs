using System.Net;
using System.Net.Http.Json;
using AnalyticsService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace AnalyticsService.Tests.Interfaces;

public class AnalyticsApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static RecordFatigueEventResource Event(DateTimeOffset occurredAt, string mineSite, string shift = "Night",
        string severity = "Warning") =>
        new(Guid.NewGuid(), Guid.NewGuid(), severity, occurredAt, shift, mineSite, "Pit 2", "CAT 797F");

    [Fact]
    public async Task PostFatigueEvent_WithValidResource_ReturnsCreated()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/analytics/fatigue-events",
            Event(MetricFactory.Start, "Api Mine", severity: "Critical"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var metric = await response.Content.ReadFromJsonAsync<FatigueMetricResource>();
        Assert.Equal("Critical", metric!.Severity);
        Assert.Equal("Api Mine", metric.MineSite);
    }

    [Fact]
    public async Task PostFatigueEvent_WithoutShift_ReturnsBadRequest()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/analytics/fatigue-events", new
        {
            operatorId = Guid.NewGuid(), alertId = Guid.NewGuid(), severity = "Warning",
            occurredAt = MetricFactory.Start, mineSite = "x", location = "y", fleet = "z"
        });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostFatigueEvent_WithUnknownSeverity_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/analytics/fatigue-events",
            Event(MetricFactory.Start, "x", severity: "Sleepy"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetFatigueTrends_ByShift_ReturnsCountsProportionsAndWarning()
    {
        // Arrange
        const string mine = "Trend Mine";
        var day = new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 3; i++)
            await _client.PostAsJsonAsync("/api/v1/analytics/fatigue-events", Event(day.AddHours(i), mine));
        await _client.PostAsJsonAsync("/api/v1/analytics/fatigue-events", Event(day, mine, "Day"));

        // Act
        var trend = await _client.GetFromJsonAsync<FatigueTrendResource>(
            $"/api/v1/analytics/fatigue-trends?groupBy=shift&mineSite={Uri.EscapeDataString(mine)}" +
            "&from=2026-03-01T00:00:00Z&to=2026-04-01T00:00:00Z");

        // Assert
        Assert.Equal(4, trend!.TotalAlerts);
        Assert.Equal("Shift", trend.GroupBy);
        Assert.Equal(0.75, trend.Groups.Single(group => group.Key == "Night").Proportion);
        Assert.False(trend.IsRepresentative);
        Assert.NotNull(trend.Warning);
    }

    [Theory]
    [InlineData("groupBy=Weather")]
    [InlineData("from=2026-04-01T00:00:00Z&to=2026-03-01T00:00:00Z")]
    public async Task GetFatigueTrends_WithInvalidFilters_ReturnsUnprocessableEntity(string queryString)
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/analytics/fatigue-trends?{queryString}");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PreventiveMeasure_RegisterQueryAndEvaluate()
    {
        // Arrange
        var appliedAt = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);
        var resource = new RegisterPreventiveMeasureResource("Night pause", "PreventivePause", "15 min at 03:00",
            appliedAt, Guid.NewGuid(), "Night", "Evaluation Mine", null, null);

        // Act
        var created = await _client.PostAsJsonAsync("/api/v1/analytics/preventive-measures", resource);
        var measure = await created.Content.ReadFromJsonAsync<PreventiveMeasureResource>();
        var byId = await _client.GetAsync($"/api/v1/analytics/preventive-measures/{measure!.Id}");
        var all = await _client.GetFromJsonAsync<List<PreventiveMeasureResource>>("/api/v1/analytics/preventive-measures");
        var evaluation = await _client.GetFromJsonAsync<MeasureEvaluationResource>(
            $"/api/v1/analytics/preventive-measures/{measure.Id}/evaluation?windowDays=14");

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byId.StatusCode);
        Assert.Contains(all!, item => item.Id == measure.Id);
        Assert.Equal("Night", measure.Shift);
        Assert.False(evaluation!.IsComparable);
        Assert.Equal(appliedAt.AddDays(-14), evaluation.Before.From);
    }

    [Fact]
    public async Task PostPreventiveMeasure_WithUnknownType_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/analytics/preventive-measures",
            new RegisterPreventiveMeasureResource("x", "Magic", null, MetricFactory.Start, Guid.NewGuid(),
                null, null, null, null));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task UnknownMeasure_ReturnsNotFound()
    {
        // Act
        var byId = await _client.GetAsync($"/api/v1/analytics/preventive-measures/{Guid.NewGuid()}");
        var evaluation = await _client.GetAsync($"/api/v1/analytics/preventive-measures/{Guid.NewGuid()}/evaluation");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, byId.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, evaluation.StatusCode);
    }
}
