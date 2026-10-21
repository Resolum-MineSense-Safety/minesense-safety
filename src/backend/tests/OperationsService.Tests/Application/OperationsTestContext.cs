using OperationsService.Application.Internal.CommandServices;
using OperationsService.Application.Internal.QueryServices;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Services;
using OperationsService.Infrastructure.Persistence.InMemory;

namespace OperationsService.Tests.Application;

/// <summary>Wires the in-memory repositories and services of the Operations context for application tests.</summary>
public class OperationsTestContext
{
    public InMemoryMiningUnitRepository MiningUnits { get; } = new();
    public InMemoryFleetRepository Fleets { get; } = new();
    public InMemoryVehicleRepository Vehicles { get; } = new();
    public InMemoryOperatorRepository Operators { get; } = new();
    public InMemoryOperatorAssignmentRepository Assignments { get; } = new();

    public MiningUnitCommandService MiningUnitCommands { get; }
    public FleetCommandService FleetCommands { get; }
    public VehicleCommandService VehicleCommands { get; }
    public OperatorCommandService OperatorCommands { get; }
    public OperatorAssignmentCommandService AssignmentCommands { get; }
    public MiningUnitQueryService MiningUnitQueries { get; }
    public FleetQueryService FleetQueries { get; }
    public VehicleQueryService VehicleQueries { get; }
    public OperatorQueryService OperatorQueries { get; }
    public OperatorAssignmentQueryService AssignmentQueries { get; }

    public OperationsTestContext()
    {
        var validator = new OperationalStructureValidator(MiningUnits, Fleets, Vehicles, Operators);
        MiningUnitCommands = new MiningUnitCommandService(MiningUnits);
        FleetCommands = new FleetCommandService(Fleets, validator);
        VehicleCommands = new VehicleCommandService(Vehicles, validator);
        OperatorCommands = new OperatorCommandService(Operators, validator);
        AssignmentCommands = new OperatorAssignmentCommandService(Assignments, validator, TimeProvider.System);
        MiningUnitQueries = new MiningUnitQueryService(MiningUnits);
        FleetQueries = new FleetQueryService(Fleets);
        VehicleQueries = new VehicleQueryService(Vehicles);
        OperatorQueries = new OperatorQueryService(Operators);
        AssignmentQueries = new OperatorAssignmentQueryService(Assignments, Vehicles);
    }

    public async Task<Fleet> RegisterFleetAsync()
    {
        var miningUnit = await MiningUnitCommands.Handle(
            new RegisterMiningUnitCommand($"UM-{Guid.NewGuid():N}", "Antamina", "Ancash"));
        return await FleetCommands.Handle(new RegisterFleetCommand(miningUnit.Id, "Acarreo 1"));
    }

    public async Task<MiningUnit> RegisterMiningUnitAsync() =>
        await MiningUnitCommands.Handle(new RegisterMiningUnitCommand($"UM-{Guid.NewGuid():N}", "Antamina", "Ancash"));

    public async Task<Operator> RegisterOperatorAsync(Guid? miningUnitId = null)
    {
        miningUnitId ??= (await RegisterMiningUnitAsync()).Id;
        return await OperatorCommands.Handle(
            new RegisterOperatorCommand($"OP-{Guid.NewGuid():N}", "Juan", "Quispe", miningUnitId.Value));
    }

    /// <summary>Id of a freshly registered, active operator (assignments require an existing operator).</summary>
    public async Task<Guid> NewOperatorIdAsync() => (await RegisterOperatorAsync()).Id;

    public async Task<Vehicle> RegisterVehicleAsync(Fleet? fleet = null)
    {
        fleet ??= await RegisterFleetAsync();
        return await VehicleCommands.Handle(
            new RegisterVehicleCommand($"CAT-{Guid.NewGuid():N}", "Caterpillar 797F", fleet.Id));
    }
}
