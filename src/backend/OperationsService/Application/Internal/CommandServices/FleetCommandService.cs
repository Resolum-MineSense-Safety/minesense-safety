using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.CommandServices;

// Sprint 1 · T14 (Joseph Huamani): register fleets (US17). Remaining: rename/deactivate and admin views in src/frontend.
public class FleetCommandService(
    IFleetRepository fleetRepository,
    OperationalStructureValidator structureValidator) : IFleetCommandService
{
    public async Task<Fleet> Handle(RegisterFleetCommand command)
    {
        var fleet = new Fleet(command);

        // T15: a fleet can only be created inside an existing mining unit.
        var miningUnit = await structureValidator.EnsureMiningUnitExistsAsync(fleet.MiningUnitId);

        var siblings = await fleetRepository.FindByMiningUnitIdAsync(fleet.MiningUnitId);
        if (siblings.Any(other => string.Equals(other.Name, fleet.Name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Fleet '{fleet.Name}' already exists in mining unit {miningUnit.BusinessCode}.");

        await fleetRepository.AddAsync(fleet);
        return fleet;
    }
}
