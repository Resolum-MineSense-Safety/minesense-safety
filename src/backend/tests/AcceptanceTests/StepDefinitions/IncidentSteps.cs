using System.Net;
using System.Text.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// US09, US10 and US11 (EP05, Incident Management Service). Incidents are opened through the REST
/// route with a new operator and alert per scenario, the same request the AlertIssued consumer makes.
/// </summary>
[Binding]
public class IncidentSteps(ApiContext api)
{
    /// <summary>One escalation period (2 minutes) plus a margin: the deadline must be exceeded.</summary>
    private static readonly TimeSpan EscalationStep = TimeSpan.FromMinutes(2) + TimeSpan.FromSeconds(1);

    private readonly Guid _operatorId = Guid.NewGuid();
    private readonly Guid _alertId = Guid.NewGuid();
    private Guid _incidentId;
    private Guid _supervisorId;

    private string IncidentPath => $"/api/v1/incidents/{_incidentId}";

    [Given("se recibió una alerta {string} de un operador")]
    [When("se recibe una alerta {string} de un operador")]
    public async Task WhenAnAlertArrives(string severity)
    {
        var incident = await api.ExpectAsync(HttpStatusCode.Created, Service.IncidentManagement, HttpMethod.Post,
            "/api/v1/incidents", new { operatorId = _operatorId, alertId = _alertId, severity });
        _incidentId = incident.GetProperty("id").GetGuid();
    }

    [When("la misma alerta se recibe de nuevo")]
    public async Task WhenTheSameAlertArrivesAgain() =>
        await api.PostAsync(Service.IncidentManagement, "/api/v1/incidents",
            new { operatorId = _operatorId, alertId = _alertId, severity = "Critical" });

    [Given("un supervisor toma el incidente")]
    [When("un supervisor toma el incidente")]
    public async Task WhenASupervisorTakesTheIncident()
    {
        _supervisorId = Guid.NewGuid();
        await api.ExpectAsync(HttpStatusCode.OK, Service.IncidentManagement, HttpMethod.Patch,
            $"{IncidentPath}/assignment", new { assigneeId = _supervisorId });
    }

    [When("otro supervisor intenta tomar el incidente")]
    public async Task WhenAnotherSupervisorTriesToTakeIt() =>
        await api.PatchAsync(Service.IncidentManagement, $"{IncidentPath}/assignment", new { assigneeId = Guid.NewGuid() });

    [Given("el supervisor registra la acción {string} con el resultado {string}")]
    [When("el supervisor registra la acción {string} con el resultado {string}")]
    public async Task WhenTheSupervisorRecordsAnAction(string type, string result)
    {
        await RecordActionAsync(type, result);
        api.AssertStatus(HttpStatusCode.Created);
    }

    [When("el supervisor registra la acción {string} sin indicar el resultado")]
    public async Task WhenTheSupervisorRecordsAnActionWithoutResult(string type) => await RecordActionAsync(type, null);

    [When("el supervisor cierra el incidente {int} minutos después de su apertura con la resolución {string}")]
    public async Task WhenTheSupervisorClosesTheIncident(int minutes, string resolution)
    {
        var incident = await GetIncidentAsync();
        var closedAt = incident.GetProperty("openedAt").GetDateTimeOffset().AddMinutes(minutes);
        await api.PatchAsync(Service.IncidentManagement, $"{IncidentPath}/closure", new { resolution, closedAt });
    }

    [When("transcurren {int} minutos sin que nadie tome el incidente")]
    [When("transcurren {int} minutos desde la apertura del incidente")]
    public async Task WhenMinutesPass(int minutes)
    {
        // The periodic worker or this request escalates each overdue period; one call per period
        // reproduces a worker that runs more often than the 2-minute deadline.
        var clock = api.Services.Clock(Service.IncidentManagement);
        var remaining = TimeSpan.FromMinutes(minutes);
        while (remaining > TimeSpan.Zero)
        {
            var step = remaining < EscalationStep ? remaining : EscalationStep;
            clock.Advance(step);
            remaining -= step;
            await api.ExpectAsync(HttpStatusCode.OK, Service.IncidentManagement, HttpMethod.Post,
                "/api/v1/incidents/overdue-escalations");
        }
    }

    [When("se consulta el escalamiento del incidente")]
    public async Task WhenTheEscalationIsQueried() =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.IncidentManagement, HttpMethod.Get, $"{IncidentPath}/escalation");

    [Then("se abre un incidente {string} con severidad {string}")]
    public async Task ThenAnIncidentIsOpened(string status, string severity)
    {
        var incident = await GetIncidentAsync();
        Assert.Equal(status, incident.GetProperty("status").GetString());
        Assert.Equal(severity, incident.GetProperty("severity").GetString());
        Assert.Equal(_alertId, incident.GetProperty("alertId").GetGuid());
    }

    [Then("el incidente aparece en la bandeja de incidentes {string} del operador")]
    public async Task ThenTheIncidentIsInTheTray(string status)
    {
        var tray = await api.ExpectAsync(HttpStatusCode.OK, Service.IncidentManagement, HttpMethod.Get,
            $"/api/v1/incidents?status={status}&operatorId={_operatorId}");
        Assert.Contains(_incidentId, tray.EnumerateArray().Select(item => item.GetProperty("id").GetGuid()));
    }

    [Then("no se abre un segundo incidente para esa alerta")]
    public async Task ThenNoSecondIncident()
    {
        Assert.True(api.Response!.IsSuccessStatusCode, api.Body);
        Assert.Equal(_incidentId, api.JsonBody.GetProperty("id").GetGuid());
        var incidents = await api.ExpectAsync(HttpStatusCode.OK, Service.IncidentManagement, HttpMethod.Get,
            $"/api/v1/incidents?operatorId={_operatorId}");
        Assert.Single(incidents.EnumerateArray());
    }

    [Then("el incidente queda {string} a ese supervisor")]
    public async Task ThenTheIncidentIsAssigned(string status)
    {
        var incident = await GetIncidentAsync();
        Assert.Equal(status, incident.GetProperty("status").GetString());
        Assert.Equal(_supervisorId, incident.GetProperty("assigneeId").GetGuid());
    }

    [Then("el incidente muestra la acción {string} con el resultado {string}")]
    public async Task ThenTheIncidentShowsTheAction(string type, string result)
    {
        var actions = (await GetIncidentAsync()).GetProperty("actions").EnumerateArray().ToList();
        Assert.Contains(actions, action =>
            action.GetProperty("actionType").GetString() == type && action.GetProperty("result").GetString() == result
            && action.GetProperty("performedBy").GetGuid() == _supervisorId);
    }

    [Then("el incidente queda {string} con un tiempo de atención de {int} segundos")]
    public async Task ThenTheIncidentIsClosed(string status, int seconds)
    {
        api.AssertStatus(HttpStatusCode.OK);
        var incident = await GetIncidentAsync();
        Assert.Equal(status, incident.GetProperty("status").GetString());
        Assert.Equal(seconds, incident.GetProperty("attentionTimeSeconds").GetDouble(), precision: 3);
        Assert.False(string.IsNullOrWhiteSpace(incident.GetProperty("resolution").GetString()));
    }

    [Then("el historial del incidente registra en orden los eventos:")]
    public async Task ThenTheHistoryHas(DataTable expected)
    {
        var history = await api.ExpectAsync(HttpStatusCode.OK, Service.IncidentManagement, HttpMethod.Get, $"{IncidentPath}/history");
        Assert.Equal(expected.Rows.Select(row => row["evento"]).ToList(),
            history.EnumerateArray().Select(item => item.GetProperty("type").GetString()!).ToList());
    }

    [Then("ya no se pueden registrar acciones en el incidente")]
    public async Task ThenNoMoreActions()
    {
        await RecordActionAsync("Other", "Seguimiento posterior");
        api.AssertStatus(HttpStatusCode.Conflict);
    }

    [Then("el escalamiento del incidente es {string} con nivel {int}")]
    public async Task ThenTheEscalationIs(string status, int level)
    {
        var escalation = await api.ExpectAsync(HttpStatusCode.OK, Service.IncidentManagement, HttpMethod.Get,
            $"{IncidentPath}/escalation");
        Assert.Equal(status, escalation.GetProperty("status").GetString());
        Assert.Equal(level, escalation.GetProperty("level").GetInt32());
    }

    [Then("se notificó a los roles {string}")]
    public void ThenTheRolesWereNotified(string roles) =>
        Assert.Equal(ApiContext.List(roles),
            api.JsonBody.GetProperty("notifiedRoles").EnumerateArray().Select(item => item.GetString()!).ToArray());

    private async Task<JsonElement> GetIncidentAsync()
    {
        var response = await api.Services.Client(Service.IncidentManagement).GetAsync(IncidentPath);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return JsonDocument.Parse(await response.Content.ReadAsStringAsync()).RootElement;
    }

    private async Task RecordActionAsync(string type, string? result)
    {
        // Performed when the supervisor took the incident: never before its opening, and in order in the history.
        var incident = await GetIncidentAsync();
        var performedAt = incident.GetProperty("assignedAt") is { ValueKind: JsonValueKind.String } assignedAt
            ? assignedAt.GetDateTimeOffset()
            : incident.GetProperty("openedAt").GetDateTimeOffset();
        await api.PostAsync(Service.IncidentManagement, $"{IncidentPath}/actions", new
        {
            actionType = type,
            description = $"Acción {type} del supervisor de turno",
            result,
            performedBy = _supervisorId,
            performedAt
        });
    }
}
