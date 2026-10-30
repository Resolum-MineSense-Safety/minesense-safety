extern alias fatigue;
extern alias alerts;

using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace MineSenseSafety.AcceptanceTests.Support;

public record AssessmentResult(Guid Id, Guid OperatorId, string RiskLevel, bool RequiresAlert);

public record AlertResult(Guid Id, Guid OperatorId, Guid AssessmentId, string Severity, string Status, DateTimeOffset? AcknowledgedAt);

/// <summary>
/// Drives the Fatigue Detection and Alerts APIs exactly as a client would, over HTTP.
/// A new pair of in-memory services is created for every scenario, so scenarios never share data.
/// The Alerts clock is a FakeTimeProvider: deadlines (30 s for critical alerts) are tested
/// without waiting.
/// </summary>
public sealed class MineSenseApi : IDisposable
{
    private readonly WebApplicationFactory<fatigue::Program> _fatigueFactory = new();
    private readonly WebApplicationFactory<alerts::Program> _alertsFactory;

    public MineSenseApi()
    {
        _alertsFactory = new WebApplicationFactory<alerts::Program>().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<TimeProvider>();
                services.AddSingleton<TimeProvider>(AlertsClock);
            }));
        Fatigue = _fatigueFactory.CreateClient();
        Alerts = _alertsFactory.CreateClient();
    }

    public FakeTimeProvider AlertsClock { get; } = new(DateTimeOffset.UtcNow);

    public HttpClient Fatigue { get; }

    public HttpClient Alerts { get; }

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

    public void Dispose()
    {
        Fatigue.Dispose();
        Alerts.Dispose();
        _fatigueFactory.Dispose();
        _alertsFactory.Dispose();
    }
}
