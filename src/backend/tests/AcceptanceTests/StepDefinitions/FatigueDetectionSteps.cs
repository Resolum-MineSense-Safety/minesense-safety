using System.Net;
using System.Net.Http.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// US03 (early fatigue detection) and US04 (risk classification), EP02.
/// Signal values follow FatigueRiskPolicy: PERCLOS ≥ 0.15, blinks ≥ 25/min or HRV &lt; 30 ms → Warning;
/// PERCLOS ≥ 0.40, or PERCLOS ≥ 0.25 with HRV &lt; 20 ms → Critical.
/// </summary>
[Binding]
public class FatigueDetectionSteps(MineSenseApi api, ScenarioState state)
{
    [Given("las señales del operador están dentro de los parámetros aceptables")]
    [Given("no existen señales relevantes de fatiga")]
    public void GivenNormalSignals() => state.Signals = api.Signals(0.05, 15, 60);

    [Given("las señales del operador presentan un patrón asociado con fatiga")]
    [Given("existen señales moderadas de fatiga")]
    public void GivenFatigueSignals() => state.Signals = api.Signals(0.20, 18, 45);

    [Given("se reconoce un posible microsueño o una condición grave")]
    public void GivenMicrosleepSignals() => state.Signals = api.Signals(0.45, 8, 18);

    [Given("no existe información suficiente para determinar el estado del operador")]
    public void GivenIncompleteSignals() => state.Signals = new
    {
        operatorId = api.OperatorId,
        monitoringSessionId = api.MonitoringSessionId,
        blinkRatePerMinute = 12.0
    };

    [When("el sistema evalúa las señales")]
    [When("el sistema completa la evaluación")]
    [When("el sistema intenta realizar la evaluación")]
    [When("el sistema determina el nivel de riesgo")]
    public async Task WhenTheSystemAssesses()
    {
        state.Response = await api.AssessAsync(state.Signals!);
        if (state.Response.StatusCode == HttpStatusCode.Created)
            state.Assessment = await state.Response.Content.ReadFromJsonAsync<AssessmentResult>();
    }

    [Then("mantiene la condición del operador como normal")]
    [Then("clasifica al operador en condición normal")]
    public void ThenNormal()
    {
        Assert.Equal(HttpStatusCode.Created, state.Response!.StatusCode);
        Assert.Equal("Normal", state.Assessment!.RiskLevel);
        Assert.False(state.Assessment.RequiresAlert);
    }

    [Then("registra la condición de riesgo correspondiente")]
    public async Task ThenRiskIsRecorded()
    {
        Assert.Equal("Warning", state.Assessment!.RiskLevel);
        Assert.True(state.Assessment.RequiresAlert);
        var stored = await api.Fatigue.GetFromJsonAsync<AssessmentResult>(
            $"/api/v1/fatigue-assessments/{state.Assessment.Id}");
        Assert.Equal(state.Assessment.Id, stored!.Id);
    }

    [Then("clasifica al operador en alerta")]
    public void ThenWarning()
    {
        Assert.Equal("Warning", state.Assessment!.RiskLevel);
        Assert.True(state.Assessment.RequiresAlert);
    }

    [Then("clasifica al operador como crítico")]
    public void ThenCritical()
    {
        Assert.Equal("Critical", state.Assessment!.RiskLevel);
        Assert.True(state.Assessment.RequiresAlert);
    }

    [Then("inicia la respuesta correspondiente al nivel crítico")]
    public async Task ThenCriticalResponseStarts()
    {
        var alert = await api.IssueAlertAsync(state.Assessment!);
        Assert.Equal("Critical", alert.Severity);
        Assert.Equal("Issued", alert.Status);
    }

    [Then("declara el resultado como no concluyente")]
    public void ThenInconclusive() => Assert.Equal(HttpStatusCode.BadRequest, state.Response!.StatusCode);

    [Then("no lo presenta como una detección confirmada")]
    public async Task ThenNotPresentedAsDetection()
    {
        var latest = await api.LatestAssessmentAsync();
        Assert.Equal(HttpStatusCode.NotFound, latest.StatusCode);
    }
}
