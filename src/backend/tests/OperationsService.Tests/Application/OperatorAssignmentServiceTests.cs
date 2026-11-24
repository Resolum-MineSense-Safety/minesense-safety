using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Tests.Application;

public class OperatorAssignmentServiceTests
{
    private static readonly DateOnly October1 = new(2026, 10, 1);
    private readonly OperationsTestContext _context = new();

    [Fact]
    public async Task CreateAssignment_ValidVehicle_IsEffectiveForThePeriod()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();
        var operatorId = await _context.NewOperatorIdAsync();

        // Act
        var assignment = await _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(operatorId, vehicle.Id, Shift.Day, October1, null));

        // Assert
        var active = await _context.AssignmentQueries.Handle(new GetActiveAssignmentByOperatorQuery(operatorId, October1.AddDays(3)));
        Assert.Equal(assignment.Id, active!.Id);
        Assert.Equal(assignment.Id, (await _context.AssignmentQueries.Handle(new GetAssignmentByIdQuery(assignment.Id)))!.Id);
    }

    [Fact]
    public async Task CreateAssignment_UnknownVehicle_IsRejected()
    {
        // Arrange
        var operatorId = await _context.NewOperatorIdAsync();

        // Act & Assert
        await Assert.ThrowsAsync<DomainNotFoundException>(() => _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(operatorId, Guid.NewGuid(), Shift.Day, October1, null)));
    }

    [Fact]
    public async Task CreateAssignment_UnknownOperator_IsRejectedAsNotFound()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();

        // Act
        var exception = await Assert.ThrowsAsync<DomainNotFoundException>(() => _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(Guid.NewGuid(), vehicle.Id, Shift.Day, October1, null)));

        // Assert
        Assert.Contains("Operator", exception.Message);
    }

    [Fact]
    public async Task CreateAssignment_InactiveOperator_IsRejected()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();
        var @operator = await _context.RegisterOperatorAsync();
        await _context.OperatorCommands.Handle(new UpdateOperatorCommand(
            @operator.Id, @operator.FirstName, @operator.LastName, @operator.MiningUnitId, OperatorStatus.Inactive));

        // Act
        var exception = await Assert.ThrowsAnyAsync<DomainException>(() => _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(@operator.Id, vehicle.Id, Shift.Day, October1, null)));

        // Assert
        Assert.Contains("Inactive", exception.Message);
    }

    [Fact]
    public async Task CreateAssignment_VehicleInMaintenance_IsRejected()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();
        await _context.VehicleCommands.Handle(
            new UpdateVehicleCommand(vehicle.Id, vehicle.Model, VehicleStatus.InMaintenance, vehicle.FleetId));
        var operatorId = await _context.NewOperatorIdAsync();

        // Act
        var exception = await Assert.ThrowsAnyAsync<DomainException>(() => _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(operatorId, vehicle.Id, Shift.Day, October1, null)));

        // Assert
        Assert.Contains("InMaintenance", exception.Message);
    }

    [Fact]
    public async Task CreateAssignment_VehicleTakenInSameShift_IsNotSaved()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();
        await _context.AssignmentCommands.Handle(new CreateAssignmentCommand(await _context.NewOperatorIdAsync(), vehicle.Id, Shift.Day, October1, null));
        var secondOperator = await _context.NewOperatorIdAsync();

        // Act
        await Assert.ThrowsAnyAsync<DomainException>(() => _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(secondOperator, vehicle.Id, Shift.Day, October1.AddDays(2), null)));

        // Assert
        Assert.Empty(await _context.AssignmentQueries.Handle(new GetAssignmentHistoryByOperatorQuery(secondOperator)));
    }

    [Fact]
    public async Task ChangeAssignedVehicle_KeepsPreviousContextInHistory()
    {
        // Arrange
        var fleet = await _context.RegisterFleetAsync();
        var firstVehicle = await _context.RegisterVehicleAsync(fleet);
        var secondVehicle = await _context.RegisterVehicleAsync(fleet);
        var operatorId = await _context.NewOperatorIdAsync();
        var current = await _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(operatorId, firstVehicle.Id, Shift.Night, October1, null));

        // Act
        var replacement = await _context.AssignmentCommands.Handle(
            new ChangeAssignedVehicleCommand(current.Id, secondVehicle.Id, October1.AddDays(5)));

        // Assert
        Assert.Equal(secondVehicle.Id, replacement!.VehicleId);
        var history = (await _context.AssignmentQueries.Handle(new GetAssignmentHistoryByOperatorQuery(operatorId))).ToList();
        Assert.Equal(2, history.Count);
        Assert.Contains(history, assignment => assignment.Id == current.Id && assignment.Status == AssignmentStatus.Superseded);
        var active = await _context.AssignmentQueries.Handle(new GetActiveAssignmentByOperatorQuery(operatorId, October1.AddDays(6)));
        Assert.Equal(replacement.Id, active!.Id);
    }

    [Fact]
    public async Task ChangeAssignedVehicle_ToVehicleTakenInSameShift_LeavesCurrentAssignmentUntouched()
    {
        // Arrange
        var fleet = await _context.RegisterFleetAsync();
        var firstVehicle = await _context.RegisterVehicleAsync(fleet);
        var takenVehicle = await _context.RegisterVehicleAsync(fleet);
        await _context.AssignmentCommands.Handle(new CreateAssignmentCommand(await _context.NewOperatorIdAsync(), takenVehicle.Id, Shift.Day, October1, null));
        var current = await _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(await _context.NewOperatorIdAsync(), firstVehicle.Id, Shift.Day, October1, null));

        // Act
        await Assert.ThrowsAnyAsync<DomainException>(() => _context.AssignmentCommands.Handle(
            new ChangeAssignedVehicleCommand(current.Id, takenVehicle.Id, October1.AddDays(3))));

        // Assert
        Assert.Equal(AssignmentStatus.Active, current.Status);
        Assert.Null(current.ValidTo);
    }

    [Fact]
    public async Task ChangeAndEnd_UnknownAssignment_ReturnNull()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();

        // Act
        var changed = await _context.AssignmentCommands.Handle(new ChangeAssignedVehicleCommand(Guid.NewGuid(), vehicle.Id, October1));
        var ended = await _context.AssignmentCommands.Handle(new EndAssignmentCommand(Guid.NewGuid(), October1));

        // Assert
        Assert.Null(changed);
        Assert.Null(ended);
    }

    [Fact]
    public async Task EndAssignment_Active_EndsIt()
    {
        // Arrange
        var vehicle = await _context.RegisterVehicleAsync();
        var assignment = await _context.AssignmentCommands.Handle(
            new CreateAssignmentCommand(await _context.NewOperatorIdAsync(), vehicle.Id, Shift.Day, October1, null));

        // Act
        var ended = await _context.AssignmentCommands.Handle(new EndAssignmentCommand(assignment.Id, October1.AddDays(10)));

        // Assert
        Assert.Equal(AssignmentStatus.Ended, ended!.Status);
        Assert.Null(await _context.AssignmentQueries.Handle(
            new GetActiveAssignmentByOperatorQuery(assignment.OperatorId, October1.AddDays(11))));
    }
}
