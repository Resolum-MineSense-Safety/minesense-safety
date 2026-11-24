namespace FeedbackReliabilityService.Application.Internal.OutboundServices;

public interface ICurrentActor
{
    Guid UserId { get; }
    bool IsInRole(string role);
    void RequireOperation(Guid operationId);
}
