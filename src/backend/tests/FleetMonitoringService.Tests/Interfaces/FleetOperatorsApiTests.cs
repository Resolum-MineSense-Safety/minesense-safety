using System.Net;
using System.Net.Http.Json;
using FleetMonitoringService.Interfaces.REST.Resources;
using Microsoft.AspNetCore.Mvc.Testing;

namespace FleetMonitoringService.Tests.Interfaces;

public class FleetOperatorsApiTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    private const string Route = "/api/v1/fleet-operators";
    private readonly HttpClient _client = factory.CreateClient();

    private async Task<MonitoredOperatorResource> RegisterAsync(
        string fleet, string shift = "Night", string name = "Luis Quispe",
        Guid? miningUnitId = null, Guid? fleetId = null)
    {
        var response = await _client.PostAsJsonAsync(Route,
            new RegisterMonitoredOperatorResource(Guid.NewGuid(), name, "CAT-797F-12", fleet, shift,
                "Tajo Principal", miningUnitId, fleetId));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<MonitoredOperatorResource>())!;
    }

    [Fact]
    public async Task Register_WithValidResource_ReturnsCreatedOperatorAtNormalRisk()
    {
        // Act
        var response = await _client.PostAsJsonAsync(Route,
            new RegisterMonitoredOperatorResource(Guid.NewGuid(), "Maria Huaman", "KOM-930E-04", "Acarreo Sur", "Day", "Botadero 2"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var resource = (await response.Content.ReadFromJsonAsync<MonitoredOperatorResource>())!;
        Assert.Equal("Normal", resource.CurrentRiskLevel);
        Assert.Equal("Current", resource.Freshness);
        Assert.False(resource.IsStale);
    }

    [Fact]
    public async Task Register_WithUnknownShift_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.PostAsJsonAsync(Route,
            new RegisterMonitoredOperatorResource(Guid.NewGuid(), "Maria Huaman", "KOM-930E-04", "Acarreo Sur", "Afternoon", "Botadero 2"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetFleetStatus_WithCombinedFilters_ReturnsOnlyMatchingOperators()
    {
        // Arrange
        var fleet = $"Flota-{Guid.NewGuid():N}";
        var match = await RegisterAsync(fleet, "Night");
        await RegisterAsync(fleet, "Day");
        await _client.PutAsJsonAsync($"{Route}/{match.OperatorId}/risk-level", new UpdateOperatorRiskLevelResource("Critical"));

        // Act
        var result = await _client.GetFromJsonAsync<List<MonitoredOperatorResource>>(
            $"{Route}?fleet={fleet}&shift=Night&riskLevel=Critical");

        // Assert
        Assert.Single(result!);
        Assert.Equal(match.OperatorId, result![0].OperatorId);
    }

    [Fact]
    public async Task GetFleetStatus_WithInvalidRiskLevel_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.GetAsync($"{Route}?riskLevel=Sleepy");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task GetByOperatorId_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var registered = await RegisterAsync($"Flota-{Guid.NewGuid():N}");

        // Act
        var found = await _client.GetAsync($"{Route}/{registered.OperatorId}");
        var missing = await _client.GetAsync($"{Route}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task ReassignVehicle_ExistingOperator_UpdatesVehicleAndLocation()
    {
        // Arrange
        var registered = await RegisterAsync($"Flota-{Guid.NewGuid():N}");

        // Act
        var response = await _client.PutAsJsonAsync($"{Route}/{registered.OperatorId}/vehicle",
            new ReassignOperatorVehicleResource("CAT-793F-02", "Rampa 3"));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = await response.Content.ReadFromJsonAsync<MonitoredOperatorResource>();
        Assert.Equal("CAT-793F-02", updated!.VehicleCode);
        Assert.Equal("Rampa 3", updated.Location);
    }

    [Fact]
    public async Task Updates_OnUnknownOperator_ReturnNotFound()
    {
        // Act
        var risk = await _client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}/risk-level", new UpdateOperatorRiskLevelResource("Warning"));
        var vehicle = await _client.PutAsJsonAsync($"{Route}/{Guid.NewGuid()}/vehicle", new ReassignOperatorVehicleResource("X-1", "Rampa 1"));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, risk.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, vehicle.StatusCode);
    }

    [Fact]
    public async Task DocumentedRoutes_ReturnFleetAndOperatorStatus()
    {
        // Arrange
        var miningUnitId = Guid.NewGuid();
        var fleetId = Guid.NewGuid();
        var registered = await RegisterAsync(
            $"Flota-{Guid.NewGuid():N}", miningUnitId: miningUnitId, fleetId: fleetId);

        // Act
        var fleet = await _client.GetFromJsonAsync<List<MonitoredOperatorResource>>(
            $"/api/v1/fleet/status?siteId={miningUnitId}&fleetId={fleetId}");
        var technicalStory = await _client.GetFromJsonAsync<List<MonitoredOperatorResource>>(
            $"/api/v1/fleet-monitoring?siteId={miningUnitId}&fleetId={fleetId}");
        var operatorStatus = await _client.GetAsync($"/api/v1/operators/{registered.OperatorId}/status");

        // Assert
        Assert.Contains(fleet!, item => item.OperatorId == registered.OperatorId);
        Assert.Contains(technicalStory!, item => item.OperatorId == registered.OperatorId);
        Assert.Equal(HttpStatusCode.OK, operatorStatus.StatusCode);
    }

    [Fact]
    public async Task GetFleetStatus_WithConflictingSiteAndMiningUnit_ReturnsUnprocessableEntity()
    {
        // Act
        var response = await _client.GetAsync(
            $"{Route}?siteId={Guid.NewGuid()}&miningUnitId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }
}
