namespace OperationsService.Domain.Model.Commands;

public record RegisterFleetCommand(Guid MiningUnitId, string Name);
