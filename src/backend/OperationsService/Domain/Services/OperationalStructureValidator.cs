using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Domain.Services;

// Sprint 1 · T15 (Nicolas Juarez): relation validation of the operational structure (US17/US19).
// Fleet -> existing MiningUnit, Vehicle -> existing Fleet, Assignment -> existing Active Vehicle.
// Remaining: cross-context check of OperatorId against IdentityAccess once it exposes operators.
public class OperationalStructureValidator(
    IMiningUnitRepository miningUnitRepository,
    IFleetRepository fleetRepository,
    IVehicleRepository vehicleRepository)
{
    public async Task<MiningUnit> EnsureMiningUnitExistsAsync(Guid miningUnitId) =>
        await miningUnitRepository.FindByIdAsync(miningUnitId)
        ?? throw new DomainException($"Mining unit {miningUnitId} does not exist.");

    public async Task<Fleet> EnsureFleetExistsAsync(Guid fleetId) =>
        await fleetRepository.FindByIdAsync(fleetId)
        ?? throw new DomainException($"Fleet {fleetId} does not exist.");

    public async Task<Vehicle> EnsureActiveVehicleAsync(Guid vehicleId)
    {
        var vehicle = await vehicleRepository.FindByIdAsync(vehicleId)
                      ?? throw new DomainException($"Vehicle {vehicleId} does not exist.");
        if (!vehicle.IsActive)
            throw new DomainException($"Vehicle {vehicle.Code} is {vehicle.Status} and cannot be assigned.");
        return vehicle;
    }
}
