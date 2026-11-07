using DeviceManagementService.Domain.Model.Aggregates;

namespace DeviceManagementService.Domain.Model.ValueObjects;

public record PreShiftAvailabilityResult(
    PreShiftAvailabilityStatus Status,
    PreShiftRecommendation Recommendation,
    IReadOnlyCollection<Device> EvaluatedDevices);
