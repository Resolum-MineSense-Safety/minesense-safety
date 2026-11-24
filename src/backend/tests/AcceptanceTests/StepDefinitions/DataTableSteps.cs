using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// Final version of the EP02/EP03 acceptance tests (section 4.8): the scenarios driven by data
/// tables and Scenario Outlines, over the same real HTTP APIs as the other steps.
/// </summary>
[Binding]
public class DataTableSteps(MineSenseApi api, ScenarioState state)
{
    private readonly Dictionary<string, AssessmentResult> _byOperator = new();
    private readonly List<(string Name, object Signals)> _shift = [];

    private static double Number(string value) => double.Parse(value, CultureInfo.InvariantCulture);

    private static bool YesNo(string value) => value.Trim().ToLowerInvariant() is "sí" or "si" or "yes";

    private object SignalsFrom(DataTableRow row, Guid? operatorId = null) => new
    {
        operatorId = operatorId ?? api.OperatorId,
        monitoringSessionId = api.MonitoringSessionId,
        perclos = Number(row["perclos"]),
        blinkRatePerMinute = Number(row["parpadeos_por_minuto"]),
        heartRateVariabilityMs = Number(row["variabilidad_cardiaca_ms"])
    };

    // ---------- US03 ----------
    [Given("el operador presenta las señales de cabina")]
    public void GivenCabinSignals(DataTable table) => state.Signals = SignalsFrom(table.Rows.Single());

    [Then("el nivel de riesgo registrado es {string}")]
    public void ThenRiskLevelIs(string level)
    {
        Assert.Equal(HttpStatusCode.Created, state.Response!.StatusCode);
        Assert.Equal(level, state.Assessment!.RiskLevel);
    }

    [Then("la evaluación requiere alerta {string}")]
    public void ThenRequiresAlert(string expected) => Assert.Equal(YesNo(expected), state.Assessment!.RequiresAlert);

    // ---------- US04 ----------
    [Given("los operadores del turno envían sus señales")]
    public void GivenShiftSignals(DataTable table)
    {
        foreach (var row in table.Rows)
            _shift.Add((row["operador"], SignalsFrom(row, Guid.NewGuid())));
    }

    [When("el sistema determina el nivel de riesgo de cada operador")]
    public async Task WhenEachOperatorIsClassified()
    {
        foreach (var (name, signals) in _shift)
            _byOperator[name] = await api.AssessAndReadAsync(signals);
    }

    [Then("los operadores quedan clasificados así")]
    public void ThenOperatorsAreClassified(DataTable table)
    {
        foreach (var row in table.Rows)
        {
            var assessment = _byOperator[row["operador"]];
            Assert.Equal(row["nivel"], assessment.RiskLevel);
            Assert.Equal(YesNo(row["requiere_alerta"]), assessment.RequiresAlert);
        }
    }

    // ---------- US05 ----------
    [Given("una evaluación con nivel de riesgo {string}")]
    public async Task GivenAssessmentWithLevel(string level)
    {
        var signals = level switch
        {
            "Critical" => api.Signals(0.45, 8, 18),
            "Warning" => api.Signals(0.20, 18, 45),
            _ => throw new ArgumentOutOfRangeException(nameof(level), level, "Only Warning or Critical raise an alert")
        };
        state.Assessment = await api.AssessAndReadAsync(signals);
        Assert.Equal(level, state.Assessment.RiskLevel);
    }

    [Then("la advertencia tiene severidad {string} y estado {string}")]
    public void ThenAlertHasSeverityAndStatus(string severity, string status)
    {
        Assert.Equal(severity, state.Alert!.Severity);
        Assert.Equal(status, state.Alert.Status);
    }

    [Then("el operador debe confirmarla en {int} segundos")]
    public async Task ThenDeadlineIs(int seconds)
    {
        // One second before the deadline the alert is still the operator's; one after, the supervisor's.
        api.AlertsClock.Advance(TimeSpan.FromSeconds(seconds - 1));
        await api.Alerts.PostAsync("/api/v1/alerts/overdue-escalations", null);
        Assert.Equal("Issued", (await api.GetAlertAsync(state.Alert!.Id)).Status);

        api.AlertsClock.Advance(TimeSpan.FromSeconds(2));
        await api.Alerts.PostAsync("/api/v1/alerts/overdue-escalations", null);
        Assert.Equal("Escalated", (await api.GetAlertAsync(state.Alert.Id)).Status);
    }

    // ---------- US06 ----------
    [Given("se emite una advertencia de severidad {string}")]
    public async Task GivenAlertWithSeverity(string severity)
    {
        await GivenAssessmentWithLevel(severity);
        state.Alert = await api.IssueAlertAsync(state.Assessment!);
    }

    [When("transcurren {int} segundos sin confirmación")]
    public async Task WhenSecondsPassWithoutAcknowledgement(int seconds)
    {
        api.AlertsClock.Advance(TimeSpan.FromSeconds(seconds));
        var response = await api.Alerts.PostAsync("/api/v1/alerts/overdue-escalations", null);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Then("el estado de la advertencia es {string}")]
    public async Task ThenAlertStatusIs(string status) =>
        Assert.Equal(status, (await api.GetAlertAsync(state.Alert!.Id)).Status);
}
