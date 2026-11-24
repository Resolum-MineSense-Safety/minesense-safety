namespace OperationsService.Domain.Model.Commands;

public record ChangeAssignedVehicleCommand(Guid AssignmentId, Guid NewVehicleId, DateOnly ChangeDate);
