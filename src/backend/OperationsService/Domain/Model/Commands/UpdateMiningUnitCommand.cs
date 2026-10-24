namespace OperationsService.Domain.Model.Commands;

public record UpdateMiningUnitCommand(Guid MiningUnitId, string Name, string Region);
