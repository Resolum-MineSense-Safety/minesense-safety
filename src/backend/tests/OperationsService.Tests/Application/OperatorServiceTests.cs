using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Tests.Application;

public class OperatorServiceTests
{
    private readonly OperationsTestContext _context = new();

    [Fact]
    public async Task RegisterOperator_InExistingMiningUnit_CanBeQueried()
    {
        // Arrange
        var miningUnit = await _context.RegisterMiningUnitAsync();

        // Act
        var @operator = await _context.OperatorCommands.Handle(
            new RegisterOperatorCommand("OP-0042", "Juan", "Quispe", miningUnit.Id));

        // Assert
        Assert.Equal(@operator.Id, (await _context.OperatorQueries.Handle(new GetOperatorByIdQuery(@operator.Id)))!.Id);
        var byUnit = await _context.OperatorQueries.Handle(new GetOperatorsQuery(miningUnit.Id));
        Assert.Equal(@operator.Id, Assert.Single(byUnit).Id);
    }

    [Fact]
    public async Task RegisterOperator_UnknownMiningUnit_IsNotFound()
    {
        // Act & Assert
        await Assert.ThrowsAsync<DomainNotFoundException>(() => _context.OperatorCommands.Handle(
            new RegisterOperatorCommand("OP-0042", "Juan", "Quispe", Guid.NewGuid())));
    }

    [Fact]
    public async Task RegisterOperator_DuplicatedEmployeeCodeIgnoringCase_IsConflictAndNotSaved()
    {
        // Arrange
        var miningUnit = await _context.RegisterMiningUnitAsync();
        await _context.OperatorCommands.Handle(new RegisterOperatorCommand("OP-0042", "Juan", "Quispe", miningUnit.Id));

        // Act
        var exception = await Assert.ThrowsAsync<DomainConflictException>(() => _context.OperatorCommands.Handle(
            new RegisterOperatorCommand("op-0042", "Pedro", "Huaman", miningUnit.Id)));

        // Assert
        Assert.Contains("OP-0042", exception.Message);
        Assert.Single(await _context.OperatorQueries.Handle(new GetOperatorsQuery(null)));
    }

    [Fact]
    public async Task UpdateOperator_ToUnknownMiningUnit_IsNotFoundAndUnknownOperatorReturnsNull()
    {
        // Arrange
        var @operator = await _context.RegisterOperatorAsync();

        // Act
        var unknownOperator = await _context.OperatorCommands.Handle(
            new UpdateOperatorCommand(Guid.NewGuid(), "Juan", "Quispe", @operator.MiningUnitId, OperatorStatus.Active));

        // Assert
        Assert.Null(unknownOperator);
        await Assert.ThrowsAsync<DomainNotFoundException>(() => _context.OperatorCommands.Handle(
            new UpdateOperatorCommand(@operator.Id, "Juan", "Quispe", Guid.NewGuid(), OperatorStatus.Active)));
    }
}
