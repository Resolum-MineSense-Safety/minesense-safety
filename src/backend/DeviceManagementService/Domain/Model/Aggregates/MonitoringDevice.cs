using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace DeviceManagementService.Domain.Model.Aggregates;

// Sprint 1 · T10 (Andreow Santiago): availability model of a monitoring device (status, battery, last heartbeat).
// Remaining: confirm the low-battery threshold with the team and add a heartbeat timeout.

/// <summary>
/// Monitoring or warning device (EP07). Its status is derived from heartbeats and
/// disconnections reported by the Edge gateway; every failure is kept in its log.
/// </summary>
public class MonitoringDevice : AggregateRoot
{
    /// <summary>Battery level below which the device is considered degraded.</summary>
    public const int LowBatteryThreshold = 20;

    private readonly List<DeviceFailure> _failures = [];

    public string SerialNumber { get; private set; }
    public DeviceType Type { get; private set; }
    public Guid? AssignedOperatorId { get; private set; }
    public string? VehicleCode { get; private set; }
    public DeviceStatus Status { get; private set; }
    public int? BatteryLevel { get; private set; }
    public DateTimeOffset? LastSeenAt { get; private set; }
    public IReadOnlyList<DeviceFailure> Failures => _failures.AsReadOnly();

    public MonitoringDevice(RegisterDeviceCommand command)
    {
        if (string.IsNullOrWhiteSpace(command.SerialNumber))
            throw new DomainException("A device must have a serial number.");
        if (!Enum.IsDefined(command.Type))
            throw new DomainException($"Unknown device type '{command.Type}'.");

        SerialNumber = command.SerialNumber.Trim();
        Type = command.Type;
        Status = DeviceStatus.Available;
    }

    public void AssignTo(Guid operatorId, string vehicleCode)
    {
        if (operatorId == Guid.Empty)
            throw new DomainException("A device must be assigned to an operator.");
        if (string.IsNullOrWhiteSpace(vehicleCode))
            throw new DomainException("A device must be assigned to a vehicle.");

        AssignedOperatorId = operatorId;
        VehicleCode = vehicleCode.Trim();
    }

    /// <summary>
    /// Registers a heartbeat. A heartbeat from a disconnected device also records its recovery.
    /// </summary>
    public void ReportHeartbeat(int? batteryLevel, DateTimeOffset at)
    {
        if (batteryLevel is < 0 or > 100)
            throw new DomainException("Battery level must be between 0 and 100.");

        if (Status == DeviceStatus.Disconnected)
            CloseOpenFailure(DeviceFailure.Disconnection, at);

        var wasLow = IsLowBattery(BatteryLevel);
        var isLow = IsLowBattery(batteryLevel);
        if (isLow && !wasLow)
            _failures.Add(new DeviceFailure(DeviceFailure.LowBattery, at, $"Battery at {batteryLevel}%"));
        if (!isLow && wasLow)
            CloseOpenFailure(DeviceFailure.LowBattery, at);

        BatteryLevel = batteryLevel;
        LastSeenAt = at;
        Status = ComputeConnectedStatus();
    }

    public void ReportDisconnection(DateTimeOffset at, string? reason)
    {
        if (Status == DeviceStatus.Disconnected)
            throw new DomainException($"Device {SerialNumber} is already disconnected.");

        _failures.Add(new DeviceFailure(DeviceFailure.Disconnection, at, reason));
        Status = DeviceStatus.Disconnected;
    }

    public void ReportRecovery(DateTimeOffset at)
    {
        if (Status != DeviceStatus.Disconnected)
            throw new DomainException($"Device {SerialNumber} is not disconnected; current status is {Status}.");

        CloseOpenFailure(DeviceFailure.Disconnection, at);
        LastSeenAt = at;
        Status = ComputeConnectedStatus();
    }

    private DeviceStatus ComputeConnectedStatus() =>
        IsLowBattery(BatteryLevel) ? DeviceStatus.Degraded : DeviceStatus.Available;

    private static bool IsLowBattery(int? batteryLevel) => batteryLevel < LowBatteryThreshold;

    private void CloseOpenFailure(string kind, DateTimeOffset at)
    {
        var index = _failures.FindLastIndex(failure => failure.Kind == kind && failure.IsOpen);
        if (index >= 0) _failures[index] = _failures[index].Close(at);
    }
}
