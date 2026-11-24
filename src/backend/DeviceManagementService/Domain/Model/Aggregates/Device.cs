using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Domain.Model.Aggregates;

public class Device : AggregateRoot
{
    private readonly List<string> _availableComponents = [];

    public string DeviceCode { get; private set; }
    public string DeviceType { get; private set; }
    public Guid OrganizationId { get; private set; }
    public Guid? AssignedOperatorId { get; private set; }
    public Guid? AssignedVehicleId { get; private set; }
    public DeviceStatus? Status { get; private set; }
    public BatteryLevel? BatteryLevel { get; private set; }
    public DateTimeOffset? ReportedAt { get; private set; }
    public IReadOnlyCollection<string> AvailableComponents => _availableComponents.AsReadOnly();

    public Device(RegisterDeviceCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.DeviceCode))
            throw new DomainException("A device code is required.");
        if (string.IsNullOrWhiteSpace(command.DeviceType))
            throw new DomainException("A device type is required.");
        if (command.OrganizationId == Guid.Empty)
            throw new DomainException("A device must belong to an organization.");

        DeviceCode = command.DeviceCode.Trim();
        DeviceType = command.DeviceType.Trim();
        OrganizationId = command.OrganizationId;
    }

    public void Assign(AssignDeviceCommand command)
    {
        if (command.DeviceId != Id)
            throw new DomainException("The assignment does not belong to this device.");
        if (command.OperatorId is null && command.VehicleId is null)
            throw new DomainException("A device must be assigned to an operator or vehicle.");
        if (command.OperatorId == Guid.Empty || command.VehicleId == Guid.Empty)
            throw new DomainException("Assignment identifiers cannot be empty.");
        if (AssignedOperatorId is not null || AssignedVehicleId is not null)
            throw new DomainException("The device already has an active assignment.");

        AssignedOperatorId = command.OperatorId;
        AssignedVehicleId = command.VehicleId;
    }

    public void ReportStatus(ReportDeviceStatusCommand command)
    {
        if (command.AvailableComponents is null)
            throw new DomainException("Available components are required.");
        if (command.AvailableComponents.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Available component names cannot be empty.");

        Status = command.Status;
        BatteryLevel = command.BatteryLevel;
        ReportedAt = command.ReportedAt;
        _availableComponents.Clear();
        _availableComponents.AddRange(command.AvailableComponents.Select(component => component.Trim()).Distinct());
    }
}
