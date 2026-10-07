namespace DeviceManagementService.Domain.Model.ValueObjects;

/// <summary>Result of verifying one device type before the shift, with the action to take when it is not Ok.</summary>
public record PreShiftCheckItem(DeviceType DeviceType, string? SerialNumber, CheckResult Result, string? RecommendedAction);
