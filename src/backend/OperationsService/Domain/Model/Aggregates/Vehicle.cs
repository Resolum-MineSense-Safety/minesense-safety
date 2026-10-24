using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Model.Aggregates;

// Sprint 1 · T13 (Ian Santisteban): vehicle aggregate (US17). Code is unique and upper-cased (e.g. CAT-797F-12).
public class Vehicle : AggregateRoot
{
    public string Code { get; private set; }
    public string Model { get; private set; }
    public Guid FleetId { get; private set; }
    public VehicleStatus Status { get; private set; }

    public Vehicle(RegisterVehicleCommand command)
    {
        MandatoryData.EnsurePresent(
            (nameof(Code), string.IsNullOrWhiteSpace(command.Code)),
            (nameof(Model), string.IsNullOrWhiteSpace(command.Model)),
            (nameof(FleetId), command.FleetId == Guid.Empty));

        Code = NormalizeCode(command.Code);
        Model = command.Model.Trim();
        FleetId = command.FleetId;
        Status = VehicleStatus.Active;
    }

    public static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();

    public bool IsActive => Status == VehicleStatus.Active;

    public void Update(UpdateVehicleCommand command)
    {
        MandatoryData.EnsurePresent(
            (nameof(Model), string.IsNullOrWhiteSpace(command.Model)),
            (nameof(FleetId), command.FleetId == Guid.Empty));

        Model = command.Model.Trim();
        Status = command.Status;
        FleetId = command.FleetId;
    }
}
