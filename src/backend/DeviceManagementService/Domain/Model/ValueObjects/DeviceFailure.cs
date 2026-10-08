namespace DeviceManagementService.Domain.Model.ValueObjects;

// Sprint 1 · T11 (Carlos Onofre): failure log entry (disconnection / low battery) with its recovery time.
// Remaining: persist the log and publish DeviceDisconnected / DeviceRecovered events to Monitoring.

/// <summary>Failure recorded for a device; it stays open until <see cref="RecoveredAt"/> is set.</summary>
public record DeviceFailure(string Kind, DateTimeOffset OccurredAt, string? Detail = null, DateTimeOffset? RecoveredAt = null)
{
    public const string Disconnection = "Disconnection";
    public const string LowBattery = "LowBattery";

    public bool IsOpen => RecoveredAt is null;

    public DeviceFailure Close(DateTimeOffset recoveredAt) => this with { RecoveredAt = recoveredAt };
}
