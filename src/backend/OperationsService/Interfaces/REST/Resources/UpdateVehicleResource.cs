namespace OperationsService.Interfaces.REST.Resources;

public record UpdateVehicleResource(string? Model, string? Status, Guid FleetId);
