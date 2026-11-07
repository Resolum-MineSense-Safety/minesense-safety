namespace OperationsService.Domain.Model.Commands;

public record RegisterMiningUnitCommand(string BusinessCode, string Name, string Region);
