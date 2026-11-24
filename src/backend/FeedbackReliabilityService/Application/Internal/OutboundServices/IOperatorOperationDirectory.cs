namespace FeedbackReliabilityService.Application.Internal.OutboundServices;

/// <summary>
/// Local copy of the operation (mining unit) each operator works in, fed by the
/// <c>FleetOperatorUpdated</c> events of Fleet Monitoring. It lets an <c>AlertIssued</c> event,
/// which only carries the operator id, be tracked under the right operation.
/// </summary>
public interface IOperatorOperationDirectory
{
    Task<Guid?> FindOperationIdAsync(Guid operatorId);

    /// <summary>Keeps the snapshot unless a newer one for the operator is already stored.</summary>
    Task UpdateAsync(Guid operatorId, Guid? operationId, DateTimeOffset updatedAt);
}
