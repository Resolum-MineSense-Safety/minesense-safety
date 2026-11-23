using System.Net;
using System.Text.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// US07 and US08 (EP04, Fleet Monitoring Service). Each scenario monitors its own fleet, named with a
/// unique suffix, and every query filters by it, so a shared deployed database does not interfere.
/// </summary>
[Binding]
public class FleetMonitoringSteps(ApiContext api)
{
    private readonly Dictionary<string, Guid> _operators = new();
    private string _fleet = string.Empty;
    private string? _lastOperator;

    [Given("el centro de control monitorea a los operadores de una flota:")]
    public async Task GivenTheControlCenterMonitors(DataTable operators)
    {
        _fleet = $"Flota-{api.Suffix}";
        foreach (var row in operators.Rows)
        {
            var operatorId = Guid.NewGuid();
            await api.ExpectAsync(HttpStatusCode.Created, Service.FleetMonitoring, HttpMethod.Post, "/api/v1/fleet-operators", new
            {
                operatorId,
                fullName = row["operador"],
                vehicleCode = row["vehiculo"],
                fleet = _fleet,
                shift = row["turno"],
                location = row["ubicacion"]
            });
            if (row["riesgo"] != "Normal")
                await ChangeRiskAsync(operatorId, row["riesgo"]);
            _operators[row["operador"]] = operatorId;
            _lastOperator = row["operador"];
        }
    }

    [When("el supervisor consulta el estado de la flota")]
    public async Task WhenTheSupervisorQueriesTheFleet() =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.FleetMonitoring, HttpMethod.Get,
            $"/api/v1/fleet/status?fleet={Uri.EscapeDataString(_fleet)}");

    [When("el supervisor filtra la flota por turno {string}, ubicación {string} y riesgo {string}")]
    public async Task WhenTheSupervisorFilters(string shift, string location, string risk) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.FleetMonitoring, HttpMethod.Get,
            $"/api/v1/fleet-operators?fleet={Uri.EscapeDataString(_fleet)}&shift={shift}" +
            $"&location={Uri.EscapeDataString(location)}&riskLevel={risk}");

    [When("el supervisor consulta el estado del operador {string}")]
    public async Task WhenTheSupervisorQueriesAnOperator(string name)
    {
        _lastOperator = name;
        await api.ExpectAsync(HttpStatusCode.OK, Service.FleetMonitoring, HttpMethod.Get,
            $"/api/v1/operators/{_operators[name]}/status");
    }

    [When("el nivel de riesgo de {string} cambia a {string}")]
    public async Task WhenTheRiskChanges(string name, string risk) => await ChangeRiskAsync(_operators[name], risk);

    [When("{string} pasa al vehículo {string} en la ubicación {string}")]
    public async Task WhenTheOperatorChangesVehicle(string name, string vehicle, string location) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.FleetMonitoring, HttpMethod.Put,
            $"/api/v1/fleet-operators/{_operators[name]}/vehicle", new { vehicleCode = vehicle, location });

    [When("transcurren {int} minutos sin novedades de {string}")]
    public void WhenMinutesPass(int minutes, string name)
    {
        _lastOperator = name;
        api.Services.Clock(Service.FleetMonitoring).Advance(TimeSpan.FromMinutes(minutes));
    }

    [Then("los operadores aparecen en este orden:")]
    public void ThenTheOperatorsAppearInOrder(DataTable expected)
    {
        var actual = api.JsonBody.EnumerateArray()
            .Select(item => (item.GetProperty("fullName").GetString(), item.GetProperty("currentRiskLevel").GetString()))
            .ToList();
        Assert.Equal(expected.Rows.Select(row => ((string?)row["operador"], (string?)row["riesgo"])).ToList(), actual);
    }

    [Then("solo ve a los operadores {string}")]
    public void ThenOnlySees(string names) =>
        Assert.Equal(ApiContext.List(names),
            api.JsonBody.EnumerateArray().Select(item => item.GetProperty("fullName").GetString()!).ToArray());

    [Then("ve el vehículo {string}, la ubicación {string} y el riesgo {string}")]
    public void ThenSeesVehicleLocationAndRisk(string vehicle, string location, string risk)
    {
        var state = api.JsonBody;
        Assert.Equal(vehicle, state.GetProperty("vehicleCode").GetString());
        Assert.Equal(location, state.GetProperty("location").GetString());
        Assert.Equal(risk, state.GetProperty("currentRiskLevel").GetString());
    }

    [Then("la información del operador figura como {string}")]
    public async Task ThenTheFreshnessIs(string freshness)
    {
        var state = await api.ExpectAsync(HttpStatusCode.OK, Service.FleetMonitoring, HttpMethod.Get,
            $"/api/v1/fleet-operators/{_operators[_lastOperator!]}");
        Assert.Equal(freshness, state.GetProperty("freshness").GetString());
        Assert.Equal(freshness == "Stale", state.GetProperty("isStale").GetBoolean());
    }

    private async Task ChangeRiskAsync(Guid operatorId, string risk) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.FleetMonitoring, HttpMethod.Put,
            $"/api/v1/fleet-operators/{operatorId}/risk-level", new { riskLevel = risk });
}
