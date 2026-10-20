using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Tests.Domain;

public class OperatorAssignmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 7, 8, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly October1 = new(2026, 10, 1);

    private static OperatorAssignment Assign(DateOnly from, DateOnly? to = null) =>
        new(new CreateAssignmentCommand(Guid.NewGuid(), Guid.NewGuid(), Shift.Day, from, to), Now);

    [Fact]
    public void Constructor_WithValidCommand_StartsActive()
    {
        // Act
        var assignment = Assign(October1, October1.AddDays(30));

        // Assert
        Assert.Equal(AssignmentStatus.Active, assignment.Status);
        Assert.Equal(Now, assignment.CreatedAt);
        Assert.True(assignment.Covers(October1.AddDays(10)));
        Assert.False(assignment.Covers(October1.AddDays(-1)));
        Assert.False(assignment.Covers(October1.AddDays(31)));
    }

    [Fact]
    public void Constructor_WithMissingData_ListsEveryMissingField()
    {
        // Arrange
        var command = new CreateAssignmentCommand(Guid.Empty, Guid.Empty, Shift.Night, default, null);

        // Act
        var exception = Assert.Throws<DomainException>(() => new OperatorAssignment(command, Now));

        // Assert
        Assert.Equal("Missing mandatory data: OperatorId, VehicleId, ValidFrom.", exception.Message);
    }

    [Fact]
    public void Constructor_WithValidToBeforeValidFrom_ThrowsDomainException()
    {
        // Act & Assert
        Assert.Throws<DomainException>(() => Assign(October1, October1.AddDays(-1)));
    }

    [Fact]
    public void Overlaps_OpenEndedAssignment_OverlapsAnyLaterPeriod()
    {
        // Arrange
        var assignment = Assign(October1);

        // Act & Assert
        Assert.True(assignment.Overlaps(October1.AddYears(1), null));
        Assert.False(assignment.Overlaps(October1.AddDays(-10), October1.AddDays(-1)));
    }

    [Fact]
    public void ChangeVehicle_KeepsPreviousAssignmentAsSupersededHistory()
    {
        // Arrange
        var current = Assign(October1, October1.AddDays(30));
        var newVehicleId = Guid.NewGuid();
        var changeDate = October1.AddDays(9);

        // Act
        var replacement = current.ChangeVehicle(newVehicleId, changeDate, Now);

        // Assert
        Assert.Equal(AssignmentStatus.Superseded, current.Status);
        Assert.Equal(changeDate.AddDays(-1), current.ValidTo);
        Assert.Equal(AssignmentStatus.Active, replacement.Status);
        Assert.Equal(newVehicleId, replacement.VehicleId);
        Assert.Equal(current.OperatorId, replacement.OperatorId);
        Assert.Equal(current.Shift, replacement.Shift);
        Assert.Equal(changeDate, replacement.ValidFrom);
        Assert.Equal(October1.AddDays(30), replacement.ValidTo);
    }

    [Fact]
    public void ChangeVehicle_OnFirstDay_ClosesPreviousAssignmentThatSameDay()
    {
        // Arrange
        var current = Assign(October1);

        // Act
        current.ChangeVehicle(Guid.NewGuid(), October1, Now);

        // Assert
        Assert.Equal(October1, current.ValidTo);
    }

    [Fact]
    public void ChangeVehicle_ToSameVehicle_ThrowsDomainException()
    {
        // Arrange
        var current = Assign(October1);

        // Act & Assert
        Assert.Throws<DomainException>(() => current.ChangeVehicle(current.VehicleId, October1.AddDays(1), Now));
    }

    [Fact]
    public void ChangeVehicle_OutsidePeriod_ThrowsDomainException()
    {
        // Arrange
        var current = Assign(October1, October1.AddDays(5));

        // Act & Assert
        Assert.Throws<DomainException>(() => current.ChangeVehicle(Guid.NewGuid(), October1.AddDays(6), Now));
        Assert.Equal(AssignmentStatus.Active, current.Status);
    }

    [Fact]
    public void ProposeVehicleChange_DoesNotModifyCurrentAssignment()
    {
        // Arrange
        var current = Assign(October1);

        // Act
        current.ProposeVehicleChange(Guid.NewGuid(), October1.AddDays(3), Now);

        // Assert
        Assert.Equal(AssignmentStatus.Active, current.Status);
        Assert.Null(current.ValidTo);
    }

    [Fact]
    public void End_ActiveAssignment_SetsEndedAndValidTo()
    {
        // Arrange
        var assignment = Assign(October1);

        // Act
        assignment.End(October1.AddDays(15));

        // Assert
        Assert.Equal(AssignmentStatus.Ended, assignment.Status);
        Assert.Equal(October1.AddDays(15), assignment.ValidTo);
    }

    [Fact]
    public void End_BeforeValidFrom_ThrowsDomainException()
    {
        // Arrange
        var assignment = Assign(October1);

        // Act & Assert
        Assert.Throws<DomainException>(() => assignment.End(October1.AddDays(-1)));
    }

    [Fact]
    public void End_AlreadyEnded_ThrowsDomainException()
    {
        // Arrange
        var assignment = Assign(October1);
        assignment.End(October1.AddDays(2));

        // Act & Assert
        Assert.Throws<DomainException>(() => assignment.End(October1.AddDays(3)));
    }
}
