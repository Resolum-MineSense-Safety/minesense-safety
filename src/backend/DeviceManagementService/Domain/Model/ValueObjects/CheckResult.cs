namespace DeviceManagementService.Domain.Model.ValueObjects;

/// <summary>Outcome of verifying one device type during the pre-shift check.</summary>
public enum CheckResult
{
    Ok,
    Warning,
    Missing
}
