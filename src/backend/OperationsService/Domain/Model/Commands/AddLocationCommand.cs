using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Model.Commands;

public record AddLocationCommand(Guid MiningUnitId, string Name, LocationKind Kind);
