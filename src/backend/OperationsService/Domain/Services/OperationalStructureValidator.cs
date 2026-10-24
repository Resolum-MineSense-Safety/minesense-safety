using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Domain.Services;

// Sprint 1 · T15 (Nicolas Juarez): relation validation of the operational structure (US17/US19).
// Fleet -> existing MiningUnit, Vehicle -> existing Fleet, Operator -> existing MiningUnit,
// Assignment -> existing Active Operator and Vehicle. Unknown references answer 404 (TS-06, TS-08).
public class OperationalStructureValidator(
    IMiningUnitRepository miningUnitRepository,
    IFleetRepository fleetRepository,
    IVehicleRepository vehicleRepository,
    IOperatorRepository operatorRepository)
{
    public async Task<MiningUnit> EnsureMiningUnitExistsAsync(Guid miningUnitId) =>
        await miningUnitRepository.FindByIdAsync(miningUnitId)
        ?? throw new DomainNotFoundException($"Mining unit {miningUnitId} does not exist.");

    public async Task<Fleet> EnsureFleetExistsAsync(Guid fleetId) =>
        await fleetRepository.FindByIdAsync(fleetId)
        ?? throw new DomainNotFoundException($"Fleet {fleetId} does not exist.");

    public async Task<Vehicle> EnsureActiveVehicleAsync(Guid vehicleId)
    {
        var vehicle = await vehicleRepository.FindByIdAsync(vehicleId)
                      ?? throw new DomainNotFoundException($"Vehicle {vehicleId} does not exist.");
        if (!vehicle.IsActive)
            throw new DomainException($"Vehicle {vehicle.Code} is {vehicle.Status} and cannot be assigned.");
        return vehicle;
    }

    public async Task<Operator> EnsureActiveOperatorAsync(Guid operatorId)
    {
        var @operator = await operatorRepository.FindByIdAsync(operatorId)
                        ?? throw new DomainNotFoundException($"Operator {operatorId} does not exist.");
        if (!@operator.IsActive)
            throw new DomainException($"Operator {@operator.EmployeeCode} is {@operator.Status} and cannot be assigned.");
        return @operator;
    }
}
