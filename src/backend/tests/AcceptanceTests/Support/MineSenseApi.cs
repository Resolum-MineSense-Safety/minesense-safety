using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Time.Testing;

namespace MineSenseSafety.AcceptanceTests.Support;

public record AssessmentResult(Guid Id, Guid OperatorId, string RiskLevel, bool RequiresAlert);

public record AlertResult(Guid Id, Guid OperatorId, Guid AssessmentId, string Severity, string Status, DateTimeOffset? AcknowledgedAt);

/// <summary>
/// Drives the Fatigue Detection and Alerts APIs exactly as a client would, over HTTP.
/// In memory a new pair of services is created for every scenario, so scenarios never share data;
/// against a deployed environment each scenario uses a new operator for the same reason.
/// The in-memory Alerts clock is a FakeTimeProvider: deadlines (30 s for critical alerts) are tested
/// without waiting.
/// </summary>
public sealed class MineSenseApi(MineSenseServices services)
{
    public FakeTimeProvider AlertsClock => services.Clock(Service.Alerts);

    public HttpClient Fatigue => services.Client(Service.FatigueDetection);

    public HttpClient Alerts => services.Client(Service.Alerts);

    public Guid OperatorId { get; } = Guid.NewGuid();

    public Guid MonitoringSessionId { get; } = Guid.NewGuid();

    public Task<HttpResponseMessage> AssessAsync(object body) =>
        Fatigue.PostAsJsonAsync("/api/v1/fatigue-assessments", body);

    public object Signals(double perclos, double blinkRatePerMinute, double heartRateVariabilityMs) => new
    {
        operatorId = OperatorId,
        monitoringSessionId = MonitoringSessionId,
        perclos,
        blinkRatePerMinute,
        heartRateVariabilityMs
    };

    public async Task<AssessmentResult> AssessAndReadAsync(object body)
    {
        var response = await AssessAsync(body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AssessmentResult>())!;
    }

    public async Task<HttpResponseMessage> LatestAssessmentAsync() =>
        await Fatigue.GetAsync($"/api/v1/fatigue-assessments/latest?operatorId={OperatorId}");

    public async Task<AlertResult> IssueAlertAsync(AssessmentResult assessment)
    {
        var response = await Alerts.PostAsJsonAsync("/api/v1/alerts", new
        {
            operatorId = assessment.OperatorId,
            assessmentId = assessment.Id,
            severity = assessment.RiskLevel
        });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<AlertResult>())!;
    }

    public async Task<AlertResult> GetAlertAsync(Guid alertId) =>
        (await Alerts.GetFromJsonAsync<AlertResult>($"/api/v1/alerts/{alertId}"))!;
}
