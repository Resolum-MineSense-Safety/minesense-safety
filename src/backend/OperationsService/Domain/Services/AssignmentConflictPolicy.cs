using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Aggregates;

namespace OperationsService.Domain.Services;

// Sprint 1 · T16 (Renato Calvo): conflict rules of US19. Only Active assignments take part; superseded/ended ones are history.
/// <summary>
/// Domain service that rejects an assignment when
/// (a) the same operator already has another Active assignment overlapping its period, or
/// (b) the same vehicle is already taken in the same shift during an overlapping period.
/// </summary>
public static class AssignmentConflictPolicy
{
    /// <param name="candidate">Assignment about to be saved.</param>
    /// <param name="existing">Assignments of the same operator and of the same vehicle.</param>
    /// <param name="ignoredAssignmentId">Assignment being replaced (vehicle change), excluded from the check.</param>
    public static void EnsureNoConflict(
        OperatorAssignment candidate,
        IEnumerable<OperatorAssignment> existing,
        Guid? ignoredAssignmentId = null)
    {
        var others = existing
            .Where(other => other.IsActive && other.Id != candidate.Id && other.Id != ignoredAssignmentId)
            .Where(other => other.Overlaps(candidate.ValidFrom, candidate.ValidTo))
            .ToList();

        var operatorConflict = others.FirstOrDefault(other => other.OperatorId == candidate.OperatorId);
        if (operatorConflict is not null)
            throw new DomainException(
                $"Assignment conflict: operator {candidate.OperatorId} already has active assignment {operatorConflict.Id} " +
                $"from {Describe(operatorConflict)}.");

        var vehicleConflict = others.FirstOrDefault(other =>
            other.VehicleId == candidate.VehicleId && other.Shift == candidate.Shift);
        if (vehicleConflict is not null)
            throw new DomainException(
                $"Assignment conflict: vehicle {candidate.VehicleId} is already assigned in the {candidate.Shift} shift " +
                $"to operator {vehicleConflict.OperatorId} (assignment {vehicleConflict.Id}, from {Describe(vehicleConflict)}).");
    }

    private static string Describe(OperatorAssignment assignment) =>
        $"{assignment.ValidFrom:yyyy-MM-dd} to {(assignment.ValidTo is { } to ? to.ToString("yyyy-MM-dd") : "open-ended")}";
}
