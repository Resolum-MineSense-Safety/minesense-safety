using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Tests.Interfaces;

public class MiningUnitsApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostMiningUnit_WithCompleteData_ReturnsCreatedUnit()
    {
        // Arrange
        var code = OperationsApiClient.UniqueCode("um");

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/mining-units",
            new RegisterMiningUnitResource(code.ToLowerInvariant(), "Antamina", "Ancash"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var miningUnit = await response.Content.ReadFromJsonAsync<MiningUnitResource>();
        Assert.Equal(code, miningUnit!.BusinessCode);
        Assert.Empty(miningUnit.Locations);
    }

    [Fact]
    public async Task PostMiningUnit_WithMissingData_ReturnsUnprocessableEntityListingFields()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/mining-units",
            new RegisterMiningUnitResource(null, "Antamina", null));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<ErrorResource>();
        Assert.Equal("Missing mandatory data: BusinessCode, Region.", error!.Message);
    }

    [Fact]
    public async Task PostMiningUnit_WithDuplicatedBusinessCode_ReturnsUnprocessableEntity()
    {
        // Arrange
        var existing = await _client.RegisterMiningUnitAsync();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/mining-units",
            new RegisterMiningUnitResource(existing.BusinessCode, "Otra", "Lima"));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(existing.BusinessCode, (await response.Content.ReadFromJsonAsync<ErrorResource>())!.Message);
    }

    [Fact]
    public async Task GetMiningUnits_ReturnsRegisteredUnits()
    {
        // Arrange
        var miningUnit = await _client.RegisterMiningUnitAsync();

        // Act
        var miningUnits = await _client.GetFromJsonAsync<List<MiningUnitResource>>("/api/v1/mining-units");

        // Assert
        Assert.Contains(miningUnits!, unit => unit.Id == miningUnit.Id);
    }

    [Fact]
    public async Task GetMiningUnitById_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var miningUnit = await _client.RegisterMiningUnitAsync();

        // Act
        var found = await _client.GetAsync($"/api/v1/mining-units/{miningUnit.Id}");
        var missing = await _client.GetAsync($"/api/v1/mining-units/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task PutMiningUnit_ValidUnknownAndIncomplete_ReturnsOkNotFoundAndUnprocessable()
    {
        // Arrange
        var miningUnit = await _client.RegisterMiningUnitAsync();

        // Act
        var updated = await _client.PutAsJsonAsync($"/api/v1/mining-units/{miningUnit.Id}",
            new UpdateMiningUnitResource("Antamina Norte", "Huari"));
        var missing = await _client.PutAsJsonAsync($"/api/v1/mining-units/{Guid.NewGuid()}",
            new UpdateMiningUnitResource("A", "B"));
        var incomplete = await _client.PutAsJsonAsync($"/api/v1/mining-units/{miningUnit.Id}",
            new UpdateMiningUnitResource("", "Huari"));

        // Assert
        Assert.Equal("Antamina Norte", (await updated.ReadAsync<MiningUnitResource>()).Name);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, incomplete.StatusCode);
    }

    [Fact]
    public async Task PostLocation_NewDuplicatedAndUnknownUnit_ReturnsCreatedUnprocessableAndNotFound()
    {
        // Arrange
        var miningUnit = await _client.RegisterMiningUnitAsync();
        var url = $"/api/v1/mining-units/{miningUnit.Id}/locations";

        // Act
        var created = await _client.PostAsJsonAsync(url, new AddLocationResource("Tajo Norte", "pit"));
        var duplicated = await _client.PostAsJsonAsync(url, new AddLocationResource("tajo norte", "Dump"));
        var unknownKind = await _client.PostAsJsonAsync(url, new AddLocationResource("Patio", "Parking"));
        var missingKind = await _client.PostAsJsonAsync(url, new AddLocationResource("Patio", null));
        var unknownUnit = await _client.PostAsJsonAsync($"/api/v1/mining-units/{Guid.NewGuid()}/locations",
            new AddLocationResource("Taller", "Workshop"));

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var location = Assert.Single((await created.Content.ReadFromJsonAsync<MiningUnitResource>())!.Locations);
        Assert.Equal(new LocationResource("Tajo Norte", "Pit"), location);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, duplicated.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownKind.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingKind.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownUnit.StatusCode);
    }
}
