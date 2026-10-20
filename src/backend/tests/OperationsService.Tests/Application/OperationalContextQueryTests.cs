using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Tests.Application;

public class OperationalContextQueryTests
{
    private static readonly DateOnly October1 = new(2026, 10, 1);
    private readonly OperationsTestContext _context = new();

    [Fact]
    public async Task Validate_AssignedOperatorAndVehicle_IsValid()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();
        var operatorId = Guid.NewGuid();
        var assignment = await _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(operatorId, vehicle.Id, Shift.Day, October1, null));

        // Act
        var result = await _context.AssignmentQueries.Handle(
            new ValidateOperationalContextQuery(operatorId, vehicle.Code.ToLowerInvariant(), October1.AddDays(1)));

        // Assert
        Assert.True(result.Valid);
        Assert.Equal(assignment.Id, result.AssignmentId);
        Assert.Null(result.Reason);
    }

    [Fact]
    public async Task Validate_EmptyOrUnknownVehicleCode_IsInvalid()
    {
        // Act
        var empty = await _context.AssignmentQueries.Handle(new ValidateOperationalContextQuery(Guid.NewGuid(), " ", October1));
        var unknown = await _context.AssignmentQueries.Handle(new ValidateOperationalContextQuery(Guid.NewGuid(), "xx-1", October1));

        // Assert
        Assert.False(empty.Valid);
        Assert.False(unknown.Valid);
        Assert.Contains("XX-1", unknown.Reason);
    }

    [Fact]
    public async Task Validate_VehicleInMaintenance_IsInvalid()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();
        await _context.VehicleCommands.Handle(
            new UpdateVehicleCommand(vehicle.Id, vehicle.Model, VehicleStatus.InMaintenance, vehicle.FleetId));

        // Act
        var result = await _context.AssignmentQueries.Handle(new ValidateOperationalContextQuery(Guid.NewGuid(), vehicle.Code, October1));

        // Assert
        Assert.False(result.Valid);
        Assert.Contains("InMaintenance", result.Reason);
    }

    [Fact]
    public async Task Validate_OperatorWithoutAssignmentOrOnOtherVehicle_IsInvalid()
    {
        // Arrange
        var fleet = await _context.RegisterFleetAsync();
        var assignedVehicle = await _context.RegisterVehicleAsync(fleet);
        var otherVehicle = await _context.RegisterVehicleAsync(fleet);
        var operatorId = Guid.NewGuid();
        await _context.AssignmentCommands.Handle(new CreateAssignmentCommand(operatorId, assignedVehicle.Id, Shift.Day, October1, null));

        // Act
        var otherVehicleResult = await _context.AssignmentQueries.Handle(
            new ValidateOperationalContextQuery(operatorId, otherVehicle.Code, October1));
        var beforeAssignment = await _context.AssignmentQueries.Handle(
            new ValidateOperationalContextQuery(operatorId, assignedVehicle.Code, October1.AddDays(-1)));

        // Assert
        Assert.False(otherVehicleResult.Valid);
        Assert.Contains("another vehicle", otherVehicleResult.Reason);
        Assert.False(beforeAssignment.Valid);
        Assert.Contains("no active assignment", beforeAssignment.Reason);
    }
}
