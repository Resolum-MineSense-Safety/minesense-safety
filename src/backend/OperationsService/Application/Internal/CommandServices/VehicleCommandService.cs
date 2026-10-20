using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.CommandServices;

// Sprint 1 · T14 (Joseph Huamani): register/update vehicles (US17). Remaining: decommission and admin views in src/frontend.
public class VehicleCommandService(
    IVehicleRepository vehicleRepository,
    OperationalStructureValidator structureValidator) : IVehicleCommandService
{
    public async Task<Vehicle> Handle(RegisterVehicleCommand command)
    {
        var vehicle = new Vehicle(command);

        // T15: a vehicle belongs to an existing fleet.
        await structureValidator.EnsureFleetExistsAsync(vehicle.FleetId);

        var duplicate = await vehicleRepository.FindByCodeAsync(vehicle.Code);
        if (duplicate is not null)
            throw new DomainException($"Vehicle code {vehicle.Code} is already registered ({duplicate.Id}).");

        await vehicleRepository.AddAsync(vehicle);
        return vehicle;
    }

    public async Task<Vehicle?> Handle(UpdateVehicleCommand command)
    {
        var vehicle = await vehicleRepository.FindByIdAsync(command.VehicleId);
        if (vehicle is null) return null;

        // T15: moving a vehicle requires the target fleet to exist.
        if (command.FleetId != vehicle.FleetId)
            await structureValidator.EnsureFleetExistsAsync(command.FleetId);

        vehicle.Update(command);
        await vehicleRepository.UpdateAsync(vehicle);
        return vehicle;
    }
}
