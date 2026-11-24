using FeedbackReliabilityService.Application.Internal;
using FeedbackReliabilityService.Application.Internal.OutboundServices;

namespace FeedbackReliabilityService.Infrastructure.Security;

public class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private System.Security.Claims.ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new FeedbackForbiddenException();

    public Guid UserId => Guid.TryParse(Principal.FindFirst("sub")?.Value, out var id) && id != Guid.Empty
        ? id : throw new FeedbackForbiddenException();

    public bool IsInRole(string role) => Principal.IsInRole(role);

    public void RequireOperation(Guid operationId)
    {
        if (operationId == Guid.Empty || !Principal.FindAll("operation_id")
            .Any(claim => Guid.TryParse(claim.Value, out var id) && id == operationId))
            throw new FeedbackForbiddenException();
    }
}
