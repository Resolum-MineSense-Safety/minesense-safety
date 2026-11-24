using System.Net;
using System.Text.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>
/// US17 and US19 (EP08, Operations Service). Business codes, vehicle codes and employee codes are
/// unique across the company, so the codes written in the features get the scenario suffix.
/// </summary>
[Binding]
public class OperationsSteps(ApiContext api)
{
    private readonly Dictionary<string, Guid> _vehicles = new();
    private readonly Dictionary<string, Guid> _operators = new();
    private readonly Dictionary<string, Guid> _assignments = new();
    private Guid _miningUnitId;
    private string _businessCode = string.Empty;
    private Guid _fleetId;
    private Guid? _lastAssignmentId;

    private static string Today => DateOnly.FromDateTime(DateTime.UtcNow).ToString("yyyy-MM-dd");

    private string Code(string code) => $"{code}-{api.Suffix}";

    [Given("una unidad minera registrada en la región {string}")]
    public async Task GivenAMiningUnit(string region)
    {
        _businessCode = Code("UM");
        var unit = await api.ExpectAsync(HttpStatusCode.Created, Service.Operations, HttpMethod.Post, "/api/v1/mining-units",
            new { businessCode = _businessCode, name = $"Unidad {api.Suffix}", region });
        _miningUnitId = unit.GetProperty("id").GetGuid();
    }

    [Given("se agregan las ubicaciones:")]
    [When("se agregan las ubicaciones:")]
    public async Task WhenLocationsAreAdded(DataTable locations)
    {
        foreach (var row in locations.Rows)
        {
            await WhenALocationIsAdded(row["nombre"], row["tipo"]);
            api.AssertStatus(HttpStatusCode.Created);
        }
    }

    [When("se agrega la ubicación {string} de tipo {string}")]
    public async Task WhenALocationIsAdded(string name, string kind) =>
        await api.PostAsync(Service.Operations, $"/api/v1/mining-units/{_miningUnitId}/locations", new { name, kind });

    [When("se registra otra unidad minera con el mismo código de negocio")]
    public async Task WhenAnotherMiningUnitWithTheSameCode() =>
        await api.PostAsync(Service.Operations, "/api/v1/mining-sites",
            new { businessCode = _businessCode.ToLowerInvariant(), name = "Otra unidad", region = "Puno" });

    [When("se registra una unidad minera con código {string}, nombre {string} y región {string}")]
    public async Task WhenAMiningUnitIsRegistered(string code, string name, string region) =>
        await api.PostAsync(Service.Operations, "/api/v1/mining-units", new { businessCode = code, name, region });

    [Given("se registra la flota {string} con los vehículos:")]
    [When("se registra la flota {string} con los vehículos:")]
    public async Task WhenAFleetIsRegistered(string name, DataTable vehicles)
    {
        var fleet = await api.ExpectAsync(HttpStatusCode.Created, Service.Operations, HttpMethod.Post, "/api/v1/fleets",
            new { miningUnitId = _miningUnitId, name });
        _fleetId = fleet.GetProperty("id").GetGuid();
        foreach (var row in vehicles.Rows)
        {
            var vehicle = await api.ExpectAsync(HttpStatusCode.Created, Service.Operations, HttpMethod.Post,
                $"/api/v1/fleets/{_fleetId}/vehicles", new { code = Code(row["codigo"]), model = row["modelo"] });
            _vehicles[row["codigo"]] = vehicle.GetProperty("id").GetGuid();
        }
    }

    [When("se registra el vehículo {string} en una flota que no existe")]
    public async Task WhenAVehicleIsRegisteredInAnUnknownFleet(string code) =>
        await api.PostAsync(Service.Operations, $"/api/v1/fleets/{Guid.NewGuid()}/vehicles", new { code = Code(code), model = "CAT 797F" });

    [Given("los operadores de la unidad minera:")]
    public async Task GivenOperators(DataTable operators)
    {
        foreach (var row in operators.Rows)
        {
            var names = row["operador"].Split(' ', 2);
            var registered = await api.ExpectAsync(HttpStatusCode.Created, Service.Operations, HttpMethod.Post, "/api/v1/operators",
                new { employeeCode = Code($"OP{_operators.Count + 1}"), firstName = names[0], lastName = names[1], miningUnitId = _miningUnitId });
            _operators[row["operador"]] = registered.GetProperty("id").GetGuid();
        }
    }

    [Given("el vehículo {string} está en mantenimiento")]
    public async Task GivenTheVehicleIsInMaintenance(string code) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.Operations, HttpMethod.Put, $"/api/v1/vehicles/{_vehicles[code]}",
            new { model = "CAT 793F", status = "InMaintenance", fleetId = _fleetId });

    [Given("{string} está asignada al vehículo {string} en el turno {string} desde hoy")]
    public async Task GivenTheOperatorIsAssigned(string name, string vehicle, string shift)
    {
        await WhenTheOperatorIsAssigned(name, vehicle, shift);
        api.AssertStatus(HttpStatusCode.Created);
        _assignments[name] = _lastAssignmentId!.Value;
    }

    [When("se asigna a {string} al vehículo {string} en el turno {string} desde hoy")]
    public async Task WhenTheOperatorIsAssigned(string name, string vehicle, string shift)
    {
        await api.PostAsync(Service.Operations, "/api/v1/operator-assignments", new
        {
            operatorId = _operators[name],
            vehicleId = _vehicles[vehicle],
            shift,
            validFrom = Today
        });
        _lastAssignmentId = api.Status == HttpStatusCode.Created ? api.JsonBody.GetProperty("id").GetGuid() : null;
    }

    [When("se cambia el vehículo de {string} a {string} desde hoy")]
    public async Task WhenTheVehicleChanges(string name, string vehicle) =>
        await api.ExpectAsync(HttpStatusCode.Created, Service.Operations, HttpMethod.Post,
            $"/api/v1/operator-assignments/{_assignments[name]}/vehicle-change",
            new { newVehicleId = _vehicles[vehicle], changeDate = Today });

    [When("el monitoreo valida si {string} opera el vehículo {string} hoy")]
    public async Task WhenMonitoringValidatesTheContext(string name, string vehicle) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.Operations, HttpMethod.Get,
            $"/api/v1/operator-assignments/context?operatorId={_operators[name]}&vehicleCode={Code(vehicle)}&date={Today}");

    [Then("la unidad minera tiene las ubicaciones:")]
    public async Task ThenTheMiningUnitHasLocations(DataTable expected)
    {
        var unit = await api.ExpectAsync(HttpStatusCode.OK, Service.Operations, HttpMethod.Get, $"/api/v1/mining-units/{_miningUnitId}");
        var locations = unit.GetProperty("locations").EnumerateArray()
            .Select(location => (location.GetProperty("name").GetString(), location.GetProperty("kind").GetString()))
            .ToList();
        Assert.Equal(expected.Rows.Select(row => ((string?)row["nombre"], (string?)row["tipo"])).ToList(), locations);
    }

    [Then("la flota tiene los vehículos activos {string}")]
    public async Task ThenTheFleetHasActiveVehicles(string codes)
    {
        var vehicles = await api.ExpectAsync(HttpStatusCode.OK, Service.Operations, HttpMethod.Get, $"/api/v1/vehicles?fleetId={_fleetId}");
        Assert.Equal(ApiContext.List(codes).Select(Code).Order(), vehicles.EnumerateArray()
            .Select(vehicle => vehicle.GetProperty("code").GetString()!).Order());
        Assert.All(vehicles.EnumerateArray(), vehicle => Assert.Equal("Active", vehicle.GetProperty("status").GetString()));
    }

    [Then("la asignación queda {string}")]
    public async Task ThenTheAssignmentIs(string status)
    {
        var assignment = await api.ExpectAsync(HttpStatusCode.OK, Service.Operations, HttpMethod.Get,
            $"/api/v1/shift-assignments/{_lastAssignmentId}");
        Assert.Equal(status, assignment.GetProperty("status").GetString());
    }

    [Then("el monitoreo puede validar que {string} opera el vehículo {string} hoy")]
    public async Task ThenMonitoringCanValidate(string name, string vehicle)
    {
        await WhenMonitoringValidatesTheContext(name, vehicle);
        Assert.True(api.JsonBody.GetProperty("valid").GetBoolean(), api.Body);
        Assert.Equal(_lastAssignmentId, api.JsonBody.GetProperty("assignmentId").GetGuid());
    }

    [Then("la validación se rechaza indicando que está asignada a otro vehículo")]
    public void ThenTheValidationIsRejected()
    {
        Assert.False(api.JsonBody.GetProperty("valid").GetBoolean());
        Assert.Contains("assigned to another vehicle", api.JsonBody.GetProperty("reason").GetString());
    }

    [Then("el historial de asignaciones de {string} registra:")]
    public async Task ThenTheAssignmentHistoryHas(string name, DataTable expected)
    {
        var history = await api.ExpectAsync(HttpStatusCode.OK, Service.Operations, HttpMethod.Get,
            $"/api/v1/operator-assignments?operatorId={_operators[name]}");
        var vehicleCodes = _vehicles.ToDictionary(pair => pair.Value, pair => pair.Key);
        var actual = history.EnumerateArray()
            .Select(item => $"{vehicleCodes[item.GetProperty("vehicleId").GetGuid()]}:{item.GetProperty("status").GetString()}")
            .Order();
        Assert.Equal(expected.Rows.Select(row => $"{row["vehiculo"]}:{row["estado"]}").Order(), actual);
    }
}
