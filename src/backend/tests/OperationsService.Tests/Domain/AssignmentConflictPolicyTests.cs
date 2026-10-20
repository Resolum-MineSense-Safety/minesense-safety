using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;
using OperationsService.Domain.Services;

namespace OperationsService.Tests.Domain;

public class AssignmentConflictPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly October1 = new(2026, 10, 1);

    private static OperatorAssignment Assign(Guid operatorId, Guid vehicleId, Shift shift, DateOnly from, DateOnly? to = null) =>
        new(new CreateAssignmentCommand(operatorId, vehicleId, shift, from, to), Now);

    [Fact]
    public void EnsureNoConflict_OperatorWithOverlappingActiveAssignment_Throws()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var existing = Assign(operatorId, Guid.NewGuid(), Shift.Day, October1, October1.AddDays(30));
        var candidate = Assign(operatorId, Guid.NewGuid(), Shift.Night, October1.AddDays(10));

        // Act
        var exception = Assert.Throws<DomainException>(() =>
            AssignmentConflictPolicy.EnsureNoConflict(candidate, [existing]));

        // Assert
        Assert.Contains("operator", exception.Message);
        Assert.Contains(existing.Id.ToString(), exception.Message);
    }

    [Fact]
    public void EnsureNoConflict_SameVehicleAndShiftOverlapping_Throws()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var existing = Assign(Guid.NewGuid(), vehicleId, Shift.Night, October1);
        var candidate = Assign(Guid.NewGuid(), vehicleId, Shift.Night, October1.AddDays(5), October1.AddDays(6));

        // Act
        var exception = Assert.Throws<DomainException>(() =>
            AssignmentConflictPolicy.EnsureNoConflict(candidate, [existing]));

        // Assert
        Assert.Contains("Night shift", exception.Message);
    }

    [Fact]
    public void EnsureNoConflict_SameVehicleInOtherShift_IsAllowed()
    {
        // Arrange
        var vehicleId = Guid.NewGuid();
        var existing = Assign(Guid.NewGuid(), vehicleId, Shift.Day, October1);
        var candidate = Assign(Guid.NewGuid(), vehicleId, Shift.Night, October1);

        // Act
        var exception = Record.Exception(() => AssignmentConflictPolicy.EnsureNoConflict(candidate, [existing]));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureNoConflict_NonOverlappingPeriods_IsAllowed()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var vehicleId = Guid.NewGuid();
        var existing = Assign(operatorId, vehicleId, Shift.Day, October1, October1.AddDays(9));
        var candidate = Assign(operatorId, vehicleId, Shift.Day, October1.AddDays(10));

        // Act
        var exception = Record.Exception(() => AssignmentConflictPolicy.EnsureNoConflict(candidate, [existing]));

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void EnsureNoConflict_EndedOrIgnoredAssignments_AreNotConflicts()
    {
        // Arrange
        var operatorId = Guid.NewGuid();
        var ended = Assign(operatorId, Guid.NewGuid(), Shift.Day, October1);
        ended.End(October1.AddDays(20));
        var replaced = Assign(operatorId, Guid.NewGuid(), Shift.Day, October1);
        var candidate = Assign(operatorId, Guid.NewGuid(), Shift.Day, October1.AddDays(5));

        // Act
        var exception = Record.Exception(() =>
            AssignmentConflictPolicy.EnsureNoConflict(candidate, [ended, replaced, candidate], replaced.Id));

        // Assert
        Assert.Null(exception);
    }
}
