using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Repositories;
using OperationsService.Domain.Services;

namespace OperationsService.Application.Internal.CommandServices;

// Sprint 1 · T14 (Joseph Huamani): register/update mining units and their locations (US17). Remaining: delete/deactivate and admin views in src/frontend.
public class MiningUnitCommandService(IMiningUnitRepository miningUnitRepository) : IMiningUnitCommandService
{
    public async Task<MiningUnit> Handle(RegisterMiningUnitCommand command)
    {
        var miningUnit = new MiningUnit(command);

        var duplicate = await miningUnitRepository.FindByBusinessCodeAsync(miningUnit.BusinessCode);
        if (duplicate is not null)
            throw new DomainException(
                $"Business code {miningUnit.BusinessCode} is already registered for mining unit '{duplicate.Name}' ({duplicate.Id}).");

        await miningUnitRepository.AddAsync(miningUnit);
        return miningUnit;
    }

    public async Task<MiningUnit?> Handle(UpdateMiningUnitCommand command)
    {
        var miningUnit = await miningUnitRepository.FindByIdAsync(command.MiningUnitId);
        if (miningUnit is null) return null;

        miningUnit.Update(command);
        await miningUnitRepository.UpdateAsync(miningUnit);
        return miningUnit;
    }

    public async Task<MiningUnit?> Handle(AddLocationCommand command)
    {
        var miningUnit = await miningUnitRepository.FindByIdAsync(command.MiningUnitId);
        if (miningUnit is null) return null;

        miningUnit.AddLocation(command.Name, command.Kind);
        await miningUnitRepository.UpdateAsync(miningUnit);
        return miningUnit;
    }
}
