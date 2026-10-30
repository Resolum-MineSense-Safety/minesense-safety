using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Tests.Interfaces;

public class OperatorsApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostOperator_ValidIncompleteAndDuplicated_ReturnsCreatedBadRequestAndConflict()
    {
        // Arrange
        var miningUnit = await _client.RegisterMiningUnitAsync();
        var code = OperationsApiClient.UniqueCode("OP");

        // Act
        var created = await _client.PostAsJsonAsync("/api/v1/operators",
            new RegisterOperatorResource(code, "Juan", "Quispe", miningUnit.Id));
        var incomplete = await _client.PostAsJsonAsync("/api/v1/operators",
            new RegisterOperatorResource(null, "Juan", null, miningUnit.Id));
        var duplicated = await _client.PostAsJsonAsync("/api/v1/operators",
            new RegisterOperatorResource(code.ToLowerInvariant(), "Pedro", "Huaman", miningUnit.Id));
        var unknownUnit = await _client.PostAsJsonAsync("/api/v1/operators",
            new RegisterOperatorResource(OperationsApiClient.UniqueCode("OP"), "Juan", "Quispe", Guid.NewGuid()));

        // Assert
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var @operator = await created.ReadAsync<OperatorResource>();
        Assert.Equal(code, @operator.EmployeeCode);
        Assert.Equal("Juan Quispe", @operator.FullName);
        Assert.Equal("Active", @operator.Status);
        Assert.Equal($"/api/v1/operators/{@operator.Id}", created.Headers.Location!.AbsolutePath);
        Assert.Equal(HttpStatusCode.BadRequest, incomplete.StatusCode);
        Assert.Equal("Missing mandatory data: EmployeeCode, LastName.",
            (await incomplete.Content.ReadFromJsonAsync<ErrorResource>())!.Message);
        Assert.Equal(HttpStatusCode.Conflict, duplicated.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownUnit.StatusCode);
    }

    [Fact]
    public async Task GetOperators_ByIdAndMiningUnit_ReturnsOkOrNotFound()
    {
        // Arrange
        var miningUnit = await _client.RegisterMiningUnitAsync();
        var @operator = await _client.RegisterOperatorAsync(miningUnit.Id);
        await _client.RegisterOperatorAsync();

        // Act
        var byId = await _client.GetAsync($"/api/v1/operators/{@operator.Id}");
        var missing = await _client.GetAsync($"/api/v1/operators/{Guid.NewGuid()}");
        var byUnit = await _client.GetFromJsonAsync<List<OperatorResource>>($"/api/v1/operators?miningUnitId={miningUnit.Id}");

        // Assert
        Assert.Equal(@operator.Id, (await byId.ReadAsync<OperatorResource>()).Id);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(@operator.Id, Assert.Single(byUnit!).Id);
    }

    [Fact]
    public async Task PutOperator_ValidUnknownAndInvalidStatus_ReturnsOkNotFoundAndBadRequest()
    {
        // Arrange
        var @operator = await _client.RegisterOperatorAsync();

        // Act
        var updated = await _client.PutAsJsonAsync($"/api/v1/operators/{@operator.Id}",
            new UpdateOperatorResource("Juana", "Quispe", @operator.MiningUnitId, "Inactive"));
        var missing = await _client.PutAsJsonAsync($"/api/v1/operators/{Guid.NewGuid()}",
            new UpdateOperatorResource("Juan", "Quispe", @operator.MiningUnitId, "Active"));
        var invalidStatus = await _client.PutAsJsonAsync($"/api/v1/operators/{@operator.Id}",
            new UpdateOperatorResource("Juan", "Quispe", @operator.MiningUnitId, "Retired"));

        // Assert
        var resource = await updated.ReadAsync<OperatorResource>();
        Assert.Equal("Juana Quispe", resource.FullName);
        Assert.Equal("Inactive", resource.Status);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidStatus.StatusCode);
    }

    [Fact]
    public async Task PostAssignment_ForInactiveOperator_ReturnsUnprocessableEntity()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();
        var @operator = await _client.RegisterOperatorAsync();
        await _client.PutAsJsonAsync($"/api/v1/operators/{@operator.Id}",
            new UpdateOperatorResource("Juan", "Quispe", @operator.MiningUnitId, "Inactive"));

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/shift-assignments",
            new CreateAssignmentResource(@operator.Id, vehicle.Id, "Day", new DateOnly(2026, 10, 1), null));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task MiningSites_IsTheReportNameOfMiningUnits()
    {
        // Act
        var created = await _client.PostAsJsonAsync("/api/v1/mining-sites",
            new RegisterMiningUnitResource(OperationsApiClient.UniqueCode("UM"), "Antamina", "Ancash"));
        var duplicated = await _client.PostAsJsonAsync("/api/v1/mining-sites",
            new RegisterMiningUnitResource((await created.ReadAsync<MiningUnitResource>()).BusinessCode, "Antamina", "Ancash"));
        var list = await _client.GetAsync("/api/v1/mining-sites");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, duplicated.StatusCode);
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
    }
}
