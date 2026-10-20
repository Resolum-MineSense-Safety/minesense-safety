namespace OperationsService.Interfaces.REST.Resources;

public record ChangeAssignedVehicleResource(Guid NewVehicleId, DateOnly ChangeDate);
