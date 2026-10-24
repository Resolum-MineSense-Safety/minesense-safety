namespace OperationsService.Domain.Model.ValueObjects;

/// <summary>
/// Active: effective for its period. Superseded: replaced by a vehicle change (kept as history).
/// Ended: closed by an administrator.
/// </summary>
public enum AssignmentStatus
{
    Active,
    Superseded,
    Ended
}
