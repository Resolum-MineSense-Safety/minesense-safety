using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.CommandServices;

// TS-07: register and update heavy machinery operators of an existing mining unit.
public class OperatorCommandService(
    IOperatorRepository operatorRepository,
    OperationalStructureValidator structureValidator) : IOperatorCommandService
{
    public async Task<Operator> Handle(RegisterOperatorCommand command)
    {
        var @operator = new Operator(command);

        await structureValidator.EnsureMiningUnitExistsAsync(@operator.MiningUnitId);

        var duplicate = await operatorRepository.FindByEmployeeCodeAsync(@operator.EmployeeCode);
        if (duplicate is not null)
            throw new DomainConflictException(
                $"Employee code {@operator.EmployeeCode} already belongs to operator {duplicate.Id}.");

        await operatorRepository.AddAsync(@operator);
        return @operator;
    }

    public async Task<Operator?> Handle(UpdateOperatorCommand command)
    {
        var @operator = await operatorRepository.FindByIdAsync(command.OperatorId);
        if (@operator is null) return null;

        if (command.MiningUnitId != @operator.MiningUnitId)
            await structureValidator.EnsureMiningUnitExistsAsync(command.MiningUnitId);

        @operator.Update(command);
        await operatorRepository.UpdateAsync(@operator);
        return @operator;
    }
}
