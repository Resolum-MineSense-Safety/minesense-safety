using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Tests.Interfaces;

public class FleetsAndVehiclesApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostFleet_ExistingAndUnknownMiningUnit_ReturnsCreatedAndUnprocessable()
    {
        // Arrange
        var miningUnit = await _client.RegisterMiningUnitAsync();

        // Act
        var created = await _client.PostAsJsonAsync("/api/v1/fleets", new RegisterFleetResource(miningUnit.Id, "Acarreo 2"));
        var orphan = await _client.PostAsJsonAsync("/api/v1/fleets", new RegisterFleetResource(Guid.NewGuid(), "Acarreo 2"));
        var duplicated = await _client.PostAsJsonAsync("/api/v1/fleets", new RegisterFleetResource(miningUnit.Id, "ACARREO 2"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, orphan.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicated.StatusCode);
    }

    [Fact]
    public async Task PostFleet_WithMissingData_ListsFields()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/fleets", new RegisterFleetResource(Guid.Empty, null));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("Missing mandatory data: MiningUnitId, Name.",
            (await response.Content.ReadFromJsonAsync<ErrorResource>())!.Message);
    }

    [Fact]
    public async Task GetFleets_ByMiningUnitAndAll_ReturnsMatchingFleets()
    {
        // Arrange
        var fleet = await _client.RegisterFleetAsync();
        await _client.RegisterFleetAsync();

        // Act
        var byUnit = await _client.GetFromJsonAsync<List<FleetResource>>($"/api/v1/fleets?miningUnitId={fleet.MiningUnitId}");
        var all = await _client.GetFromJsonAsync<List<FleetResource>>("/api/v1/fleets");

        // Assert
        Assert.Equal(fleet.Id, Assert.Single(byUnit!).Id);
        Assert.True(all!.Count >= 2);
    }

    [Fact]
    public async Task GetFleetById_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var fleet = await _client.RegisterFleetAsync();

        // Act
        var found = await _client.GetAsync($"/api/v1/fleets/{fleet.Id}");
        var missing = await _client.GetAsync($"/api/v1/fleets/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PostVehicle_ValidUnknownFleetDuplicatedAndIncomplete_ReturnsExpectedStatus()
    {
        // Arrange
        var fleet = await _client.RegisterFleetAsync();
        var code = OperationsApiClient.UniqueCode("CAT");

        // Act
        var created = await _client.PostAsJsonAsync("/api/v1/vehicles", new RegisterVehicleResource(code, "797F", fleet.Id));
        var orphan = await _client.PostAsJsonAsync("/api/v1/vehicles",
            new RegisterVehicleResource(OperationsApiClient.UniqueCode("CAT"), "797F", Guid.NewGuid()));
        var duplicated = await _client.PostAsJsonAsync("/api/v1/vehicles",
            new RegisterVehicleResource(code.ToLowerInvariant(), "797F", fleet.Id));
        var incomplete = await _client.PostAsJsonAsync("/api/v1/vehicles", new RegisterVehicleResource(null, null, fleet.Id));

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var vehicle = await created.Content.ReadFromJsonAsync<VehicleResource>();
        Assert.Equal("Active", vehicle!.Status);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, orphan.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicated.StatusCode);
        Assert.Contains(code, (await duplicated.Content.ReadFromJsonAsync<ErrorResource>())!.Message);
        Assert.Equal("Missing mandatory data: Code, Model.",
            (await incomplete.Content.ReadFromJsonAsync<ErrorResource>())!.Message);
    }

    [Fact]
    public async Task GetVehicles_ByFleetAndAll_ReturnsMatchingVehicles()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();
        await _client.RegisterVehicleAsync();

        // Act
        var byFleet = await _client.GetFromJsonAsync<List<VehicleResource>>($"/api/v1/vehicles?fleetId={vehicle.FleetId}");
        var all = await _client.GetFromJsonAsync<List<VehicleResource>>("/api/v1/vehicles");

        // Assert
        Assert.Equal(vehicle.Id, Assert.Single(byFleet!).Id);
        Assert.True(all!.Count >= 2);
    }

    [Fact]
    public async Task GetVehicleById_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();

        // Act
        var found = await _client.GetAsync($"/api/v1/vehicles/{vehicle.Id}");
        var missing = await _client.GetAsync($"/api/v1/vehicles/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(vehicle.Code, (await found.ReadAsync<VehicleResource>()).Code);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PutVehicle_ValidUnknownAndInvalidStatus_ReturnsOkNotFoundAndUnprocessable()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();
        var otherFleet = await _client.RegisterFleetAsync();

        // Act
        var updated = await _client.PutAsJsonAsync($"/api/v1/vehicles/{vehicle.Id}",
            new UpdateVehicleResource("797F Tier 4", "InMaintenance", otherFleet.Id));
        var missing = await _client.PutAsJsonAsync($"/api/v1/vehicles/{Guid.NewGuid()}",
            new UpdateVehicleResource("797F", "Active", otherFleet.Id));
        var invalidStatus = await _client.PutAsJsonAsync($"/api/v1/vehicles/{vehicle.Id}",
            new UpdateVehicleResource("797F", "Broken", otherFleet.Id));
        var unknownFleet = await _client.PutAsJsonAsync($"/api/v1/vehicles/{vehicle.Id}",
            new UpdateVehicleResource("797F", "Active", Guid.NewGuid()));

        // Assert
        var resource = await updated.ReadAsync<VehicleResource>();
        Assert.Equal("InMaintenance", resource.Status);
        Assert.Equal(otherFleet.Id, resource.FleetId);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, invalidStatus.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownFleet.StatusCode);
    }
}
