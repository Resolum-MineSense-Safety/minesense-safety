using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Model.Commands;

public record UpdateVehicleCommand(Guid VehicleId, string Model, VehicleStatus Status, Guid FleetId);
