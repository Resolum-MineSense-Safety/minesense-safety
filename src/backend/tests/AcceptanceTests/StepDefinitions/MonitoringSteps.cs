using System.Globalization;
using System.Net;
using System.Text.Json;
using MineSenseSafety.AcceptanceTests.Support;
using Reqnroll;

namespace MineSenseSafety.AcceptanceTests.StepDefinitions;

/// <summary>Operator, cabin devices and monitoring session of one scenario.</summary>
public class CabinState
{
    public Guid OperatorId { get; } = Guid.NewGuid();

    public string VehicleCode { get; set; } = string.Empty;

    public Guid OrganizationId { get; } = Guid.NewGuid();

    public List<Guid> DeviceIds { get; } = [];

    public string? LastDeviceCode { get; set; }

    public Guid? SessionId { get; set; }
}

/// <summary>
/// US01, US02 (EP01, Monitoring Service) and EP06 (Device Management Service). Device types and
/// statuses follow PreShiftAvailabilityPolicy: a camera and a wearable, both Available and with
/// battery, are needed to start monitoring; a battery under the threshold only warns.
/// </summary>
[Binding]
public class MonitoringSteps(ApiContext api, CabinState cabin)
{
    [Given("un operador que inicia su turno en un camión de acarreo")]
    public void GivenAnOperatorStartsTheShift() => cabin.VehicleCode = $"CA-{api.Suffix}";

    [Given("los dispositivos de cabina del operador están disponibles con {int} % de batería")]
    public async Task GivenCabinDevicesAvailable(int battery)
    {
        await RegisterAssignedDeviceAsync("camera", "Available", battery);
        await RegisterAssignedDeviceAsync("wearable", "Available", battery);
    }

    [Given("el operador tiene asignados los dispositivos:")]
    public async Task GivenTheOperatorHasDevices(DataTable devices)
    {
        foreach (var row in devices.Rows)
            await RegisterAssignedDeviceAsync(row["tipo"], row["estado"], decimal.Parse(row["bateria"], CultureInfo.InvariantCulture));
    }

    [Given("un dispositivo {string} registrado")]
    public async Task GivenARegisteredDevice(string type) => await RegisterDeviceAsync(type);

    [When("se registra otro dispositivo con el mismo código")]
    public async Task WhenAnotherDeviceWithTheSameCode() =>
        await api.PostAsync(Service.DeviceManagement, "/api/v1/devices",
            new { deviceCode = cabin.LastDeviceCode, deviceType = "camera", organizationId = cabin.OrganizationId });

    [When("se asigna la misma cámara a otro operador")]
    public async Task WhenTheSameCameraIsAssignedToAnotherOperator() =>
        await api.PutAsync(Service.DeviceManagement, $"/api/v1/devices/{cabin.DeviceIds[0]}/assignment",
            new { operatorId = Guid.NewGuid() });

    [When("se verifica la disponibilidad pre-turno con un umbral de batería de {int} %")]
    public async Task WhenThePreShiftCheckRuns(int threshold) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.DeviceManagement, HttpMethod.Get,
            $"/api/v1/devices/pre-shift-availability?operatorId={cabin.OperatorId}&batteryThreshold={threshold}");

    [Then("el resultado de la verificación es {string} con la recomendación {string}")]
    public void ThenTheCheckResultIs(string status, string recommendation)
    {
        var body = api.JsonBody;
        Assert.Equal(status, body.GetProperty("status").GetString());
        Assert.Equal(recommendation, body.GetProperty("recommendation").GetString());
    }

    [Then("la verificación informa que falta el dispositivo {string}")]
    public void ThenTheCheckReportsTheMissingType(string type) =>
        Assert.Contains(type, api.JsonBody.GetProperty("missingDeviceTypes").EnumerateArray().Select(item => item.GetString()));

    [Given("el monitoreo está activo con las señales {string}")]
    public async Task GivenMonitoringIsActive(string signals)
    {
        await WhenMonitoringStarts(signals);
        api.AssertStatus(HttpStatusCode.Created);
    }

    [When("se inicia el monitoreo con las señales {string}")]
    public async Task WhenMonitoringStarts(string signals)
    {
        await api.PostAsync(Service.Monitoring, "/api/v1/monitoring-sessions", new
        {
            operatorId = cabin.OperatorId,
            vehicleCode = cabin.VehicleCode,
            availableSignals = ApiContext.List(signals)
        });
        if (api.Status == HttpStatusCode.Created)
            cabin.SessionId = api.JsonBody.GetProperty("id").GetGuid();
    }

    [Given("el gateway de la cabina informa la pérdida de la señal {string}")]
    [When("el gateway de la cabina informa la pérdida de la señal {string}")]
    public async Task WhenASignalIsLost(string signal) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.Monitoring, HttpMethod.Post,
            $"/api/v1/monitoring-sessions/{cabin.SessionId}/signal-losses", new { signal });

    [When("el gateway de la cabina informa la recuperación de la señal {string}")]
    public async Task WhenASignalIsRestored(string signal) =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.Monitoring, HttpMethod.Post,
            $"/api/v1/monitoring-sessions/{cabin.SessionId}/signal-restorations", new { signal });

    [When("el operador finaliza el monitoreo")]
    public async Task WhenTheOperatorFinishesMonitoring() =>
        await api.ExpectAsync(HttpStatusCode.OK, Service.Monitoring, HttpMethod.Patch,
            $"/api/v1/monitoring-sessions/{cabin.SessionId}/finish");

    [Then("la sesión de monitoreo queda en estado {string}")]
    public async Task ThenTheSessionStatusIs(string status)
    {
        var session = await api.ExpectAsync(HttpStatusCode.OK, Service.Monitoring, HttpMethod.Get,
            $"/api/v1/monitoring-sessions/{cabin.SessionId}");
        Assert.Equal(status, session.GetProperty("status").GetString());
    }

    [Then("el panel del operador muestra el mensaje {string}")]
    public async Task ThenTheDashboardShows(string message)
    {
        var current = await api.ExpectAsync(HttpStatusCode.OK, Service.Monitoring, HttpMethod.Get,
            $"/api/v1/monitoring-sessions/current?operatorId={cabin.OperatorId}");
        Assert.Equal(message, current.GetProperty("message").GetString());
    }

    [Then("el historial de señales de la sesión registra:")]
    public async Task ThenTheSignalHistoryHas(DataTable expected)
    {
        var session = await api.ExpectAsync(HttpStatusCode.OK, Service.Monitoring, HttpMethod.Get,
            $"/api/v1/monitoring-sessions/{cabin.SessionId}");
        var events = session.GetProperty("signalEvents").EnumerateArray()
            .Select(item => (item.GetProperty("signal").GetString(), item.GetProperty("type").GetString()))
            .ToList();
        Assert.Equal(expected.Rows.Select(row => ((string?)row["senal"], (string?)row["evento"])).ToList(), events);
    }

    [Then("la sesión ya no acepta reportes de señales")]
    public async Task ThenTheSessionRejectsSignals()
    {
        await api.PostAsync(Service.Monitoring, $"/api/v1/monitoring-sessions/{cabin.SessionId}/signal-losses",
            new { signal = "HeartRate" });
        api.AssertStatus(HttpStatusCode.UnprocessableEntity);
    }

    [Then("el inicio del monitoreo se rechaza porque los dispositivos no están disponibles")]
    public void ThenStartIsRejectedForDevices()
    {
        api.AssertStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Contains("not available", api.Body, StringComparison.OrdinalIgnoreCase);
    }

    [Then("el inicio del monitoreo se rechaza porque ya existe una sesión abierta")]
    public void ThenStartIsRejectedForOpenSession()
    {
        api.AssertStatus(HttpStatusCode.UnprocessableEntity);
        Assert.Contains("already has an open monitoring session", api.Body);
    }

    private async Task<Guid> RegisterDeviceAsync(string type)
    {
        cabin.LastDeviceCode = $"{type.ToUpperInvariant()}-{api.Suffix}-{cabin.DeviceIds.Count + 1}";
        var device = await api.ExpectAsync(HttpStatusCode.Created, Service.DeviceManagement, HttpMethod.Post,
            "/api/v1/devices", new { deviceCode = cabin.LastDeviceCode, deviceType = type, organizationId = cabin.OrganizationId });
        var deviceId = device.GetProperty("id").GetGuid();
        cabin.DeviceIds.Add(deviceId);
        return deviceId;
    }

    private async Task RegisterAssignedDeviceAsync(string type, string status, decimal battery)
    {
        var deviceId = await RegisterDeviceAsync(type);
        await api.ExpectAsync(HttpStatusCode.OK, Service.DeviceManagement, HttpMethod.Put,
            $"/api/v1/devices/{deviceId}/assignment", new { operatorId = cabin.OperatorId });
        await api.ExpectAsync(HttpStatusCode.Accepted, Service.DeviceManagement, HttpMethod.Post,
            $"/api/v1/devices/{deviceId}/status", new
            {
                status,
                batteryLevel = battery,
                reportedAt = DateTimeOffset.UtcNow,
                availableComponents = new[] { type == "camera" ? "eye-tracking" : "heart-rate" }
            });
    }
}
