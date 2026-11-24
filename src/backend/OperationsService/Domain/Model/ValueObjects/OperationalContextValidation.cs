namespace OperationsService.Domain.Model.ValueObjects;

/// <summary>
/// Result of checking whether an operator may run a monitoring session on a vehicle on a date.
/// </summary>
public record OperationalContextValidation(bool Valid, Guid? AssignmentId, string? Reason)
{
    public static OperationalContextValidation Accepted(Guid assignmentId) => new(true, assignmentId, null);
    public static OperationalContextValidation Rejected(string reason) => new(false, null, reason);
}
