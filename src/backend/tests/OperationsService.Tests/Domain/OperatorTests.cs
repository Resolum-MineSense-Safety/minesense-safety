using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Tests.Domain;

public class OperatorTests
{
    [Fact]
    public void Constructor_WithValidData_NormalizesEmployeeCodeAndStartsActive()
    {
        // Act
        var @operator = new Operator(new RegisterOperatorCommand(" op-0042 ", " Juan ", " Quispe ", Guid.NewGuid()));

        // Assert
        Assert.Equal("OP-0042", @operator.EmployeeCode);
        Assert.Equal("Juan Quispe", @operator.FullName);
        Assert.Equal(OperatorStatus.Active, @operator.Status);
        Assert.True(@operator.IsActive);
    }

    [Fact]
    public void Constructor_WithMissingData_ListsEveryMissingFieldAsValidationError()
    {
        // Act
        var exception = Assert.Throws<DomainValidationException>(() =>
            new Operator(new RegisterOperatorCommand(" ", "Juan", "", Guid.Empty)));

        // Assert
        Assert.Equal("Missing mandatory data: EmployeeCode, LastName, MiningUnitId.", exception.Message);
    }

    [Fact]
    public void Update_ChangesDataAndStatusButKeepsEmployeeCode()
    {
        // Arrange
        var @operator = new Operator(new RegisterOperatorCommand("OP-1", "Juan", "Quispe", Guid.NewGuid()));
        var newMiningUnitId = Guid.NewGuid();

        // Act
        @operator.Update(new UpdateOperatorCommand(@operator.Id, "Juana", "Quispe Mamani", newMiningUnitId, OperatorStatus.Inactive));

        // Assert
        Assert.Equal("OP-1", @operator.EmployeeCode);
        Assert.Equal("Juana Quispe Mamani", @operator.FullName);
        Assert.Equal(newMiningUnitId, @operator.MiningUnitId);
        Assert.False(@operator.IsActive);
    }

    [Fact]
    public void Update_WithMissingName_ThrowsNamingTheField()
    {
        // Arrange
        var @operator = new Operator(new RegisterOperatorCommand("OP-1", "Juan", "Quispe", Guid.NewGuid()));

        // Act
        var exception = Assert.Throws<DomainValidationException>(() =>
            @operator.Update(new UpdateOperatorCommand(@operator.Id, "", "Quispe", @operator.MiningUnitId, OperatorStatus.Active)));

        // Assert
        Assert.Equal("Missing mandatory data: FirstName.", exception.Message);
    }
}
