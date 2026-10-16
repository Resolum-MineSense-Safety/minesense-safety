using System.Net;
using System.Net.Http.Json;
using FeedbackReliabilityService.Application.Internal.OutboundServices;
using FeedbackReliabilityService.Domain.Model.ValueObjects;
using FeedbackReliabilityService.Interfaces.REST.Resources;

namespace FeedbackReliabilityService.Tests.Interfaces;

public class FeedbackApiTests
{
    private static async Task<Guid> TrackAsync(FeedbackApiFactory factory, Guid operatorId)
    {
        var id = Guid.NewGuid();
        factory.Alerts[id] = new SourceAlert(id, operatorId, DateTimeOffset.UtcNow.AddMinutes(-1));
        using var service = factory.Client(Guid.NewGuid(), "Service");
        var response = await service.PutAsJsonAsync($"/api/v1/feedback-alerts/{id}", new { factory.OperationId });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return id;
    }

    [Theory]
    [InlineData("CONFIRMED", null)]
    [InlineData("FALSE_ALARM", "No fatigue")]
    [InlineData("INCONCLUSIVE", null)]
    public async Task Report_ValidOwner_ReturnsCreatedAndReadableFeedback(string classification, string? reason)
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        var owner = Guid.NewGuid();
        var id = await TrackAsync(factory, owner);
        using var client = factory.Client(owner, "Operator");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/alerts/{id}/feedback", new { classification, reason, reportedBy = owner });
        var created = await response.Content.ReadFromJsonAsync<EvaluationResource>();
        var fetched = await client.GetFromJsonAsync<EvaluationResource>(response.Headers.Location);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Equal(classification, created!.EffectiveClassification);
        Assert.Equal(created.AlertId, fetched!.AlertId);
        Assert.Equal(owner, Assert.Single(fetched.Feedback).ReportedBy);
    }

    [Theory]
    [InlineData("WRONG")]
    [InlineData("0")]
    [InlineData("Confirmed")]
    public async Task Report_InvalidClassification_Returns400(string classification)
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        var owner = Guid.NewGuid();
        using var client = factory.Client(owner, "Operator");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/alerts/{Guid.NewGuid()}/feedback", new { classification, reportedBy = owner });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Report_MissingReporter_Returns400()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        using var client = factory.Client(Guid.NewGuid(), "Operator");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/alerts/{Guid.NewGuid()}/feedback", new { classification = "CONFIRMED" });

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Report_FalseAlarmWithoutReason_Returns422()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        var owner = Guid.NewGuid();
        var id = await TrackAsync(factory, owner);
        using var client = factory.Client(owner, "Operator");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/alerts/{id}/feedback", new { classification = "FALSE_ALARM", reportedBy = owner });

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Report_Duplicate_Returns409()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        var owner = Guid.NewGuid();
        var id = await TrackAsync(factory, owner);
        using var client = factory.Client(owner, "Operator");
        var resource = new { classification = "CONFIRMED", reportedBy = owner };
        await client.PostAsJsonAsync($"/api/v1/alerts/{id}/feedback", resource);

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/alerts/{id}/feedback", resource);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Report_UnknownAlert_Returns404()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        var owner = Guid.NewGuid();
        using var client = factory.Client(owner, "Operator");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/alerts/{Guid.NewGuid()}/feedback", new { classification = "CONFIRMED", reportedBy = owner });

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Report_ImpersonationOrDifferentOwner_Returns403(bool impersonate)
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        var owner = Guid.NewGuid();
        var attacker = Guid.NewGuid();
        var id = await TrackAsync(factory, owner);
        using var client = factory.Client(attacker, "Operator");

        // Act
        var response = await client.PostAsJsonAsync($"/api/v1/alerts/{id}/feedback",
            new { classification = "CONFIRMED", reportedBy = impersonate ? owner : attacker });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Requests_WithoutValidJwt_Return401()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        using var client = factory.CreateClient();

        // Act
        var anonymous = await client.GetAsync($"/api/v1/alerts/{Guid.NewGuid()}/feedback");
        client.DefaultRequestHeaders.Authorization = new("Bearer", "forged-token");
        var forged = await client.GetAsync($"/api/v1/alerts/{Guid.NewGuid()}/feedback");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, forged.StatusCode);
    }

    [Fact]
    public async Task Supervisor_ReviewChangesMetricWithoutDoublingSample()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        var owner = Guid.NewGuid();
        var id = await TrackAsync(factory, owner);
        await TrackAsync(factory, owner);
        using var operatorClient = factory.Client(owner, "Operator");
        await operatorClient.PostAsJsonAsync($"/api/v1/alerts/{id}/feedback",
            new { classification = "FALSE_ALARM", reason = "Dust", reportedBy = owner });
        var supervisorId = Guid.NewGuid();
        using var supervisor = factory.Client(supervisorId, "Supervisor");

        // Act
        var review = await supervisor.PostAsJsonAsync($"/api/v1/alerts/{id}/feedback/reviews",
            new { classification = "CONFIRMED", observation = "Evidence checked", reviewedBy = supervisorId });
        var metric = await supervisor.GetFromJsonAsync<ReliabilityMetric>(MetricUrl(factory.OperationId));

        // Assert
        Assert.Equal(HttpStatusCode.OK, review.StatusCode);
        Assert.Equal(1, metric!.SampleSize);
        Assert.Equal(1, metric.PendingAlerts);
        Assert.Equal(1, metric.ConfirmedProportion);
        Assert.Equal(0, metric.FalseAlarms);
    }

    [Fact]
    public async Task Queue_IncludesPendingAndSupportsPagination()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        await TrackAsync(factory, Guid.NewGuid());
        await TrackAsync(factory, Guid.NewGuid());
        using var supervisor = factory.Client(Guid.NewGuid(), "Supervisor");
        var period = Period();

        // Act
        var result = await supervisor.GetFromJsonAsync<EvaluationPageResource>(
            $"/api/v1/feedback-alerts?operationId={factory.OperationId}&{period}&status=PENDING&pageSize=1");

        // Assert
        Assert.Equal(2, result!.Total);
        Assert.Equal("PENDING", Assert.Single(result.Items).Status);
    }

    [Fact]
    public async Task Metrics_Empty_Returns200AndZeros()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        using var manager = factory.Client(Guid.NewGuid(), "Manager");

        // Act
        var metric = await manager.GetFromJsonAsync<ReliabilityMetric>(MetricUrl(factory.OperationId));

        // Assert
        Assert.Equal(0, metric!.SampleSize);
        Assert.True(metric.InsufficientSample);
    }

    [Fact]
    public async Task Metrics_OutsideScope_Returns403()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        using var manager = factory.Client(Guid.NewGuid(), "Manager");

        // Act
        var response = await manager.GetAsync(MetricUrl(Guid.NewGuid()));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Operator_CannotReviewOrReadManagementMetrics()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        var owner = Guid.NewGuid();
        using var client = factory.Client(owner, "Operator");

        // Act
        var metric = await client.GetAsync(MetricUrl(factory.OperationId));
        var review = await client.PostAsJsonAsync($"/api/v1/alerts/{Guid.NewGuid()}/feedback/reviews",
            new { classification = "CONFIRMED", observation = "Checked", reviewedBy = owner });

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, metric.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, review.StatusCode);
    }

    [Fact]
    public async Task Metrics_InvalidPeriod_Returns400()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        using var manager = factory.Client(Guid.NewGuid(), "Manager");

        // Act
        var response = await manager.GetAsync($"/api/v1/analytics/alert-reliability?operationId={factory.OperationId}&from=2026-10-15&to=2026-10-01");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Track_RetryIsIdempotentButCannotRelocateAlert()
    {
        // Arrange
        using var factory = new FeedbackApiFactory();
        var id = await TrackAsync(factory, Guid.NewGuid());
        using var service = factory.Client(Guid.NewGuid(), "Service");

        // Act
        var response = await service.PutAsJsonAsync($"/api/v1/feedback-alerts/{id}", new { factory.OperationId });
        var denied = await service.PutAsJsonAsync($"/api/v1/feedback-alerts/{id}", new { operationId = Guid.NewGuid() });

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
    }

    private static string Period() => $"from={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(-1).ToString("O"))}&to={Uri.EscapeDataString(DateTimeOffset.UtcNow.AddDays(1).ToString("O"))}";
    private static string MetricUrl(Guid operation) => $"/api/v1/analytics/alert-reliability?operationId={operation}&{Period()}";
}
