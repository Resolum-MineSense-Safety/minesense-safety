using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;

namespace OperationsService.Domain.Services;

public interface IMiningUnitCommandService
{
    Task<MiningUnit> Handle(RegisterMiningUnitCommand command);
    Task<MiningUnit?> Handle(UpdateMiningUnitCommand command);
    Task<MiningUnit?> Handle(AddLocationCommand command);
}
