using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MonitoringService.Application.Internal.OutboundServices;
using MonitoringService.Interfaces.REST.Resources;
using MonitoringService.Tests.Fakes;

namespace MonitoringService.Tests.Interfaces;

public class MonitoringSessionsApiTests : IClassFixture<WebApplicationFactory<Program>>
{
    private const string BaseUrl = "/api/v1/monitoring-sessions";

    private readonly HttpClient _client;

    public MonitoringSessionsApiTests(WebApplicationFactory<Program> factory)
    {
        // Replace the HTTP adapter towards the Operations service with a fake.
        _client = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IOperationalContextService>();
            services.AddSingleton<IOperationalContextService, FakeOperationalContextService>();
        })).CreateClient();
    }

    private static StartMonitoringResource StartResource(Guid operatorId, string vehicleCode = "CAM-001",
        params string[] signals) =>
        new(operatorId, vehicleCode, Guid.NewGuid(), signals.Length == 0 ? ["EyeTracking", "HeartRate"] : signals);

    private async Task<MonitoringSessionResource> StartSessionAsync(Guid operatorId, params string[] signals)
    {
        var response = await _client.PostAsJsonAsync(BaseUrl, StartResource(operatorId, "CAM-001", signals));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<MonitoringSessionResource>())!;
    }

    [Fact]
    public async Task PostSession_WithValidContext_ReturnsCreatedActiveSession()
    {
        // Arrange
        var operatorId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsJsonAsync(BaseUrl, StartResource(operatorId));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var session = await response.Content.ReadFromJsonAsync<MonitoringSessionResource>();
        Assert.Equal("Active", session!.Status);
        Assert.Equal(operatorId, session.OperatorId);
        Assert.Equal("CAM-001", session.VehicleCode);
        Assert.NotEqual(Guid.Empty, session.AssignmentId);
        Assert.Empty(session.MissingSignals);
    }

    [Fact]
    public async Task PostSession_WithInvalidContext_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync(BaseUrl,
            StartResource(Guid.NewGuid(), FakeOperationalContextService.UnassignedPrefix + "-1"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(FakeOperationalContextService.InvalidReason, await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task PostSession_WithUnknownSignal_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync(BaseUrl,
            StartResource(Guid.NewGuid(), "CAM-001", "Temperature"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task PostSession_SecondOpenSessionForOperator_ReturnsUnprocessableEntity()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await StartSessionAsync(operatorId);

        // Act
        var response = await _client.PostAsJsonAsync(BaseUrl, StartResource(operatorId, "CAM-002"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetSessionById_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var session = await StartSessionAsync(Guid.NewGuid());

        // Act
        var found = await _client.GetAsync($"{BaseUrl}/{session.Id}");
        var missing = await _client.GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(session.Id, (await found.Content.ReadFromJsonAsync<MonitoringSessionResource>())!.Id);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task GetSessions_FilteredByStatus_ReturnsOnlyMatchingSessions()
    {
        // Arrange
        var partial = await StartSessionAsync(Guid.NewGuid(), "HeartRate");
        var active = await StartSessionAsync(Guid.NewGuid());

        // Act
        var partialSessions = await _client.GetFromJsonAsync<List<MonitoringSessionResource>>($"{BaseUrl}?status=partial");
        var allSessions = await _client.GetFromJsonAsync<List<MonitoringSessionResource>>(BaseUrl);

        // Assert
        Assert.Contains(partialSessions!, session => session.Id == partial.Id);
        Assert.DoesNotContain(partialSessions!, session => session.Id == active.Id);
        Assert.All(partialSessions!, session => Assert.Equal("Partial", session.Status));
        Assert.Contains(allSessions!, session => session.Id == active.Id);
    }

    [Fact]
    public async Task GetSessions_WithUnknownStatus_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}?status=Sleeping");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetCurrent_AllSignals_ReturnsActiveMessage()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var session = await StartSessionAsync(operatorId);

        // Act
        var status = await _client.GetFromJsonAsync<MonitoringStatusResource>($"{BaseUrl}/current?operatorId={operatorId}");

        // Assert
        Assert.Equal(session.Id, status!.SessionId);
        Assert.Equal("Active", status.Status);
        Assert.Equal("Monitoreo activo", status.Message);
        Assert.Empty(status.MissingSignals);
    }

    [Fact]
    public async Task GetCurrent_WearableMissing_ReturnsPartialNamingSignal()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        await StartSessionAsync(operatorId, "EyeTracking");

        // Act
        var status = await _client.GetFromJsonAsync<MonitoringStatusResource>($"{BaseUrl}/current?operatorId={operatorId}");

        // Assert
        Assert.Equal("Partial", status!.Status);
        Assert.Equal(["HeartRate"], status.MissingSignals);
        Assert.Equal("Monitoreo parcial: falta frecuencia cardiaca (wearable)", status.Message);
    }

    [Fact]
    public async Task GetCurrent_AllSignalsLost_ReturnsNotAvailable()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var session = await StartSessionAsync(operatorId, "EyeTracking");
        await _client.PostAsJsonAsync($"{BaseUrl}/{session.Id}/signal-losses", new SignalReportResource("EyeTracking"));

        // Act
        var status = await _client.GetFromJsonAsync<MonitoringStatusResource>($"{BaseUrl}/current?operatorId={operatorId}");

        // Assert
        Assert.Equal("Unavailable", status!.Status);
        Assert.Equal("Monitoreo no disponible", status.Message);
    }

    [Fact]
    public async Task GetCurrent_UnknownOperator_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync($"{BaseUrl}/current?operatorId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task SignalLossThenRestoration_ReturnsPartialThenActive()
    {
        // Arrange
        var session = await StartSessionAsync(Guid.NewGuid());

        // Act
        var lost = await _client.PostAsJsonAsync($"{BaseUrl}/{session.Id}/signal-losses",
            new SignalReportResource("HeartRate"));
        var restored = await _client.PostAsJsonAsync($"{BaseUrl}/{session.Id}/signal-restorations",
            new SignalReportResource("HeartRate"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, lost.StatusCode);
        Assert.Equal("Partial", (await lost.Content.ReadFromJsonAsync<MonitoringSessionResource>())!.Status);
        Assert.Equal(HttpStatusCode.OK, restored.StatusCode);
        var restoredSession = (await restored.Content.ReadFromJsonAsync<MonitoringSessionResource>())!;
        Assert.Equal("Active", restoredSession.Status);
        Assert.Equal(2, restoredSession.SignalEvents.Count);
        Assert.Equal("Lost", restoredSession.SignalEvents[0].Type);
    }

    [Fact]
    public async Task SignalReports_BrokenRules_ReturnUnprocessableEntity()
    {
        // Arrange
        var session = await StartSessionAsync(Guid.NewGuid(), "EyeTracking");

        // Act
        var lossOfMissingSignal = await _client.PostAsJsonAsync($"{BaseUrl}/{session.Id}/signal-losses",
            new SignalReportResource("HeartRate"));
        var restorationOfAvailableSignal = await _client.PostAsJsonAsync($"{BaseUrl}/{session.Id}/signal-restorations",
            new SignalReportResource("EyeTracking"));
        var unknownSignal = await _client.PostAsJsonAsync($"{BaseUrl}/{session.Id}/signal-losses",
            new SignalReportResource("Temperature"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, lossOfMissingSignal.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, restorationOfAvailableSignal.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownSignal.StatusCode);
    }

    [Fact]
    public async Task Stop_ThenStopAgain_ReturnsOkThenUnprocessableEntity()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var session = await StartSessionAsync(operatorId);

        // Act
        var stopped = await _client.PostAsync($"{BaseUrl}/{session.Id}/stop", null);
        var stoppedAgain = await _client.PostAsync($"{BaseUrl}/{session.Id}/stop", null);
        var current = await _client.GetFromJsonAsync<MonitoringStatusResource>($"{BaseUrl}/current?operatorId={operatorId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, stopped.StatusCode);
        var stoppedSession = (await stopped.Content.ReadFromJsonAsync<MonitoringSessionResource>())!;
        Assert.Equal("Stopped", stoppedSession.Status);
        Assert.NotNull(stoppedSession.StoppedAt);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, stoppedAgain.StatusCode);
        Assert.Equal("Stopped", current!.Status);
        Assert.Equal("Monitoreo detenido", current.Message);
    }

    [Fact]
    public async Task Actions_OnUnknownSession_ReturnNotFound()
    {
        // Arrange
        var unknownId = Guid.NewGuid();

        // Act
        var lost = await _client.PostAsJsonAsync($"{BaseUrl}/{unknownId}/signal-losses",
            new SignalReportResource("HeartRate"));
        var restored = await _client.PostAsJsonAsync($"{BaseUrl}/{unknownId}/signal-restorations",
            new SignalReportResource("HeartRate"));
        var stopped = await _client.PostAsync($"{BaseUrl}/{unknownId}/stop", null);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, lost.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, restored.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, stopped.StatusCode);
    }
}
