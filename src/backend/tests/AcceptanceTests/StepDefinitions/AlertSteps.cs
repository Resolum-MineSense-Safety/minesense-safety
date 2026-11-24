using System.Net;
using System.Net.Http.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// US05 (preventive warnings) and US06 (acknowledging a warning), EP03.
/// A warning is the alert that Alerts issues for an assessment of Fatigue Detection.
/// </summary>
[Binding]
public class AlertSteps(MineSenseApi api, ScenarioState state)
{
    [Given("se confirma una condición de fatiga")]
    public async Task GivenConfirmedFatigue() =>
        state.Assessment = await api.AssessAndReadAsync(api.Signals(0.20, 18, 45));

    [Given("se detecta un posible microsueño")]
    public async Task GivenPossibleMicrosleep() =>
        state.Assessment = await api.AssessAndReadAsync(api.Signals(0.45, 8, 18));

    [Given("el operador recibe una advertencia")]
    public async Task GivenOperatorReceivesWarning()
    {
        await GivenConfirmedFatigue();
        state.Alert = await api.IssueAlertAsync(state.Assessment!);
    }

    [Given("el operador confirma una advertencia")]
    public async Task GivenOperatorAcknowledgedWarning()
    {
        await GivenOperatorReceivesWarning();
        await WhenOperatorAcknowledges();
        Assert.Equal(HttpStatusCode.OK, state.Response!.StatusCode);
    }

    [Given("se emite una advertencia crítica")]
    public async Task GivenCriticalWarningIssued()
    {
        await GivenPossibleMicrosleep();
        state.Alert = await api.IssueAlertAsync(state.Assessment!);
    }

    [When("el sistema genera la advertencia")]
    public async Task WhenTheSystemIssuesTheWarning() =>
        state.Alert = await api.IssueAlertAsync(state.Assessment!);

    [When("el operador confirma que la atiende")]
    public async Task WhenOperatorAcknowledges() =>
        state.Response = await api.Alerts.PostAsync($"/api/v1/alerts/{state.Alert!.Id}/acknowledgements", null);

    [When("sus señales regresan a una condición aceptable")]
    public async Task WhenSignalsRecover() =>
        state.Assessment = await api.AssessAndReadAsync(api.Signals(0.05, 15, 60));

    [When("transcurre el tiempo establecido sin recibir confirmación")]
    public async Task WhenDeadlinePassesWithoutAcknowledgement()
    {
        // Critical alerts must be acknowledged within 30 seconds (AlertAcknowledgementPolicy).
        api.AlertsClock.Advance(TimeSpan.FromSeconds(31));
        state.Response = await api.Alerts.PostAsync("/api/v1/alerts/overdue-escalations", null);
        Assert.Equal(HttpStatusCode.OK, state.Response.StatusCode);
    }

    [Then("el operador recibe una señal perceptible acorde con el nivel de riesgo")]
    public async Task ThenOperatorGetsWarningMatchingRisk()
    {
        Assert.Equal("Warning", state.Alert!.Severity);
        Assert.Equal("Issued", state.Alert.Status);
        var operatorAlerts = await api.Alerts.GetFromJsonAsync<List<AlertResult>>(
            $"/api/v1/alerts?operatorId={api.OperatorId}");
        Assert.Contains(operatorAlerts!, alert => alert.Id == state.Alert.Id);
    }

    [Then("utiliza los medios disponibles necesarios para comunicar la urgencia")]
    public void ThenCriticalUrgencyIsCommunicated()
    {
        // The cloud marks the alert as critical (30 s acknowledgement deadline); the Edge gateway
        // turns that severity into sound, vibration and the cabin screen.
        Assert.Equal("Critical", state.Alert!.Severity);
        Assert.Equal("Issued", state.Alert.Status);
    }

    [Then("el sistema registra el evento como reconocido")]
    public async Task ThenEventIsAcknowledged()
    {
        Assert.Equal(HttpStatusCode.OK, state.Response!.StatusCode);
        var alert = await api.GetAlertAsync(state.Alert!.Id);
        Assert.Equal("Acknowledged", alert.Status);
        Assert.NotNull(alert.AcknowledgedAt);
    }

    [Then("el sistema registra la recuperación")]
    public async Task ThenRecoveryIsRecorded()
    {
        var latest = await (await api.LatestAssessmentAsync()).Content.ReadFromJsonAsync<AssessmentResult>();
        Assert.Equal("Normal", latest!.RiskLevel);
        Assert.False(latest.RequiresAlert);
        Assert.Equal("Acknowledged", (await api.GetAlertAsync(state.Alert!.Id)).Status);
    }

    [Then("el evento permanece pendiente de confirmación")]
    public async Task ThenEventStaysUnacknowledged() =>
        Assert.Null((await api.GetAlertAsync(state.Alert!.Id)).AcknowledgedAt);

    [Then("el sistema comunica la ausencia de confirmación al supervisor")]
    public async Task ThenSupervisorIsNotified()
    {
        Assert.Equal("Escalated", (await api.GetAlertAsync(state.Alert!.Id)).Status);
        var supervisorQueue = await api.Alerts.GetFromJsonAsync<List<AlertResult>>("/api/v1/alerts/status/Escalated");
        Assert.Contains(supervisorQueue!, alert => alert.Id == state.Alert!.Id);
    }

    // Edge-only scenario (tagged @ignore): documented here so the binding report stays complete.
    [Given("uno de los medios de advertencia no funciona")]
    [Given("existe otro medio de advertencia disponible")]
    [When("el sistema debe comunicar el riesgo")]
    [Then("utiliza el medio disponible para emitir la advertencia")]
    [Then("informa la falla del medio que no funciona")]
    public void EdgeOnlyStep() => throw new PendingStepException("Verified by the Edge gateway tests.");
}
