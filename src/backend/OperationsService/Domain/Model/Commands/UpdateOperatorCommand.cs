using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Model.Commands;

public record UpdateOperatorCommand(Guid OperatorId, string FirstName, string LastName, Guid MiningUnitId, OperatorStatus Status);
