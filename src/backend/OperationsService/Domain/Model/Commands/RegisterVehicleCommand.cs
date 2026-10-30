namespace OperationsService.Domain.Model.Commands;

public record RegisterVehicleCommand(string Code, string Model, Guid FleetId);
