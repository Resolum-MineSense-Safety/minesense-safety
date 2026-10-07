namespace MonitoringService.Application.Internal.OutboundServices;

/// <summary>
/// Result of validating that an operator is assigned to a vehicle for the shift.
/// </summary>
public record OperationalContext(bool Valid, Guid? AssignmentId, string? Reason)
{
    public static OperationalContext Invalid(string reason) => new(false, null, reason);
}
