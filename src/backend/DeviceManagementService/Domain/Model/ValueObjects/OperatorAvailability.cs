using DeviceManagementService.Domain.Model.Aggregates;

namespace DeviceManagementService.Domain.Model.ValueObjects;

public record OperatorAvailability(
    Guid OperatorId,
    OperatorMonitoringStatus Status,
    IReadOnlyCollection<Device> Devices);
