using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Model.Commands;

public record CreateAssignmentCommand(Guid OperatorId, Guid VehicleId, Shift Shift, DateOnly ValidFrom, DateOnly? ValidTo);
