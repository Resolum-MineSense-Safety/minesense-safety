namespace OperationsService.Interfaces.REST.Resources;

public record RegisterVehicleResource(string? Code, string? Model, Guid FleetId);
