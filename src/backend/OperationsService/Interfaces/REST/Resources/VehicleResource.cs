namespace OperationsService.Interfaces.REST.Resources;

public record VehicleResource(Guid Id, string Code, string Model, Guid FleetId, string Status);
