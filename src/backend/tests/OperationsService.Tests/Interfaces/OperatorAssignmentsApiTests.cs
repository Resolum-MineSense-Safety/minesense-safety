using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Tests.Interfaces;

public class OperatorAssignmentsApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly DateOnly October1 = new(2026, 10, 1);
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task PostAssignment_ValidVehicle_ReturnsCreatedActiveAssignment()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();
        var operatorId = Guid.NewGuid();

        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/operator-assignments",
            new CreateAssignmentResource(operatorId, vehicle.Id, "night", October1, October1.AddDays(30)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var assignment = await response.Content.ReadFromJsonAsync<OperatorAssignmentResource>();
        Assert.Equal("Active", assignment!.Status);
        Assert.Equal("Night", assignment.Shift);
        Assert.Equal(October1.AddDays(30), assignment.ValidTo);
    }

    [Fact]
    public async Task PostAssignment_InvalidRequests_ReturnUnprocessableEntity()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();

        // Act
        var missingShift = await _client.PostAsJsonAsync("/api/v1/operator-assignments",
            new CreateAssignmentResource(Guid.NewGuid(), vehicle.Id, null, October1, null));
        var unknownVehicle = await _client.PostAsJsonAsync("/api/v1/operator-assignments",
            new CreateAssignmentResource(Guid.NewGuid(), Guid.NewGuid(), "Day", October1, null));
        var missingOperator = await _client.PostAsJsonAsync("/api/v1/operator-assignments",
            new CreateAssignmentResource(Guid.Empty, vehicle.Id, "Day", October1, null));

        // Assert
        Assert.Equal("Missing mandatory data: Shift.", (await missingShift.Content.ReadFromJsonAsync<ErrorResource>())!.Message);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, unknownVehicle.StatusCode);
        Assert.Equal("Missing mandatory data: OperatorId.", (await missingOperator.Content.ReadFromJsonAsync<ErrorResource>())!.Message);
    }

    [Fact]
    public async Task PostAssignment_ConflictingOperatorOrVehicleShift_IsReportedAndNotSaved()
    {
        // Arrange
        var fleet = await _client.RegisterFleetAsync();
        var vehicle = await _client.RegisterVehicleAsync(fleet.Id);
        var otherVehicle = await _client.RegisterVehicleAsync(fleet.Id);
        var operatorId = Guid.NewGuid();
        var secondOperator = Guid.NewGuid();
        await _client.CreateAssignmentAsync(operatorId, vehicle.Id, October1);

        // Act
        var operatorConflict = await _client.PostAsJsonAsync("/api/v1/operator-assignments",
            new CreateAssignmentResource(operatorId, otherVehicle.Id, "Night", October1.AddDays(3), null));
        var vehicleConflict = await _client.PostAsJsonAsync("/api/v1/operator-assignments",
            new CreateAssignmentResource(secondOperator, vehicle.Id, "Day", October1.AddDays(3), null));

        // Assert
        Assert.Equal(HttpStatusCode.UnprocessableEntity, operatorConflict.StatusCode);
        Assert.Contains("conflict", (await operatorConflict.Content.ReadFromJsonAsync<ErrorResource>())!.Message);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, vehicleConflict.StatusCode);
        var history = await _client.GetFromJsonAsync<List<OperatorAssignmentResource>>(
            $"/api/v1/operator-assignments?operatorId={secondOperator}");
        Assert.Empty(history!);
    }

    [Fact]
    public async Task GetAssignmentById_ExistingAndUnknown_ReturnsOkAndNotFound()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();
        var assignment = await _client.CreateAssignmentAsync(Guid.NewGuid(), vehicle.Id, October1);

        // Act
        var found = await _client.GetAsync($"/api/v1/operator-assignments/{assignment.Id}");
        var missing = await _client.GetAsync($"/api/v1/operator-assignments/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, found.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task GetActiveAssignment_WithAndWithoutAssignment_ReturnsOkAndNotFound()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();
        var operatorId = Guid.NewGuid();
        var assignment = await _client.CreateAssignmentAsync(operatorId, vehicle.Id, October1);

        // Act
        var active = await _client.GetAsync($"/api/v1/operator-assignments/active?operatorId={operatorId}&date=2026-10-05");
        var activeToday = await _client.GetAsync($"/api/v1/operator-assignments/active?operatorId={operatorId}");
        var beforeStart = await _client.GetAsync($"/api/v1/operator-assignments/active?operatorId={operatorId}&date=2026-09-30");
        var unknownOperator = await _client.GetAsync($"/api/v1/operator-assignments/active?operatorId={Guid.NewGuid()}");

        // Assert
        Assert.Equal(assignment.Id, (await active.ReadAsync<OperatorAssignmentResource>()).Id);
        Assert.Equal(HttpStatusCode.OK, activeToday.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, beforeStart.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownOperator.StatusCode);
    }

    [Fact]
    public async Task PostVehicleChange_KeepsPreviousAssignmentInHistory()
    {
        // Arrange
        var fleet = await _client.RegisterFleetAsync();
        var firstVehicle = await _client.RegisterVehicleAsync(fleet.Id);
        var secondVehicle = await _client.RegisterVehicleAsync(fleet.Id);
        var operatorId = Guid.NewGuid();
        var current = await _client.CreateAssignmentAsync(operatorId, firstVehicle.Id, October1);

        // Act
        var response = await _client.PostAsJsonAsync($"/api/v1/operator-assignments/{current.Id}/vehicle-change",
            new ChangeAssignedVehicleResource(secondVehicle.Id, October1.AddDays(5)));

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var replacement = await response.Content.ReadFromJsonAsync<OperatorAssignmentResource>();
        Assert.Equal(secondVehicle.Id, replacement!.VehicleId);
        var history = await _client.GetFromJsonAsync<List<OperatorAssignmentResource>>(
            $"/api/v1/operator-assignments?operatorId={operatorId}");
        Assert.Equal(2, history!.Count);
        var previous = Assert.Single(history, assignment => assignment.Id == current.Id);
        Assert.Equal("Superseded", previous.Status);
        Assert.Equal(October1.AddDays(4), previous.ValidTo);
    }

    [Fact]
    public async Task PostVehicleChange_UnknownAssignmentAndSameVehicle_ReturnsNotFoundAndUnprocessable()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();
        var current = await _client.CreateAssignmentAsync(Guid.NewGuid(), vehicle.Id, October1);

        // Act
        var missing = await _client.PostAsJsonAsync($"/api/v1/operator-assignments/{Guid.NewGuid()}/vehicle-change",
            new ChangeAssignedVehicleResource(vehicle.Id, October1));
        var sameVehicle = await _client.PostAsJsonAsync($"/api/v1/operator-assignments/{current.Id}/vehicle-change",
            new ChangeAssignedVehicleResource(vehicle.Id, October1.AddDays(1)));

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, sameVehicle.StatusCode);
    }

    [Fact]
    public async Task PostEnd_ActiveUnknownAndAlreadyEnded_ReturnsOkNotFoundAndUnprocessable()
    {
        // Arrange
        var vehicle = await _client.RegisterVehicleAsync();
        var assignment = await _client.CreateAssignmentAsync(Guid.NewGuid(), vehicle.Id, October1);
        var url = $"/api/v1/operator-assignments/{assignment.Id}/end";

        // Act
        var ended = await _client.PostAsJsonAsync(url, new EndAssignmentResource(October1.AddDays(10)));
        var again = await _client.PostAsJsonAsync(url, new EndAssignmentResource(October1.AddDays(11)));
        var missing = await _client.PostAsJsonAsync($"/api/v1/operator-assignments/{Guid.NewGuid()}/end",
            new EndAssignmentResource(October1));

        // Assert
        var resource = await ended.ReadAsync<OperatorAssignmentResource>();
        Assert.Equal("Ended", resource.Status);
        Assert.Equal(October1.AddDays(10), resource.ValidTo);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missing.StatusCode);
    }

    [Fact]
    public async Task GetContext_AssignedAndUnassignedVehicle_ReturnsValidAndReason()
    {
        // Arrange
        var fleet = await _client.RegisterFleetAsync();
        var vehicle = await _client.RegisterVehicleAsync(fleet.Id);
        var otherVehicle = await _client.RegisterVehicleAsync(fleet.Id);
        var operatorId = Guid.NewGuid();
        var assignment = await _client.CreateAssignmentAsync(operatorId, vehicle.Id, October1);
        var baseUrl = $"/api/v1/operator-assignments/context?operatorId={operatorId}";

        // Act
        var valid = await _client.GetFromJsonAsync<OperationalContextResource>(
            $"{baseUrl}&vehicleCode={vehicle.Code}&date=2026-10-07");
        var otherVehicleResult = await _client.GetFromJsonAsync<OperationalContextResource>(
            $"{baseUrl}&vehicleCode={otherVehicle.Code}&date=2026-10-07");
        var unknownVehicle = await _client.GetFromJsonAsync<OperationalContextResource>($"{baseUrl}&vehicleCode=NOPE-1");
        var noCode = await _client.GetFromJsonAsync<OperationalContextResource>(baseUrl);

        // Assert
        Assert.True(valid!.Valid);
        Assert.Equal(assignment.Id, valid.AssignmentId);
        Assert.False(otherVehicleResult!.Valid);
        Assert.NotNull(otherVehicleResult.Reason);
        Assert.False(unknownVehicle!.Valid);
        Assert.False(noCode!.Valid);
    }
}
