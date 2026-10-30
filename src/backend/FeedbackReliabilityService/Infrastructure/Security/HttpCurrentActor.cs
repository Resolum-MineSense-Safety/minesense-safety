using FeedbackReliabilityService.Application.Internal;
using FeedbackReliabilityService.Application.Internal.OutboundServices;

namespace FeedbackReliabilityService.Infrastructure.Security;

public class HttpCurrentActor(IHttpContextAccessor accessor) : ICurrentActor
{
    private System.Security.Claims.ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new FeedbackForbiddenException();

    public Guid UserId => Guid.TryParse(Principal.FindFirst("sub")?.Value, out var id) && id != Guid.Empty
        ? id : throw new FeedbackForbiddenException();

    /// <summary>
    /// Feedback roles mapped to the roles of Identity &amp; Access: a SafetyManager acts as Manager, and
    /// an Administrator or Supervisor may register alerts like the internal Service caller.
    /// </summary>
    public bool IsInRole(string role) => role switch
    {
        "Manager" => Principal.IsInRole("Manager") || Principal.IsInRole("SafetyManager"),
        "Service" => Principal.IsInRole("Service") || Principal.IsInRole("Administrator") || Principal.IsInRole("Supervisor"),
        _ => Principal.IsInRole(role)
    };

    /// <summary>The operation scope comes as <c>operation_id</c> or as the <c>mining_unit</c> claim of Identity &amp; Access.</summary>
    public void RequireOperation(Guid operationId)
    {
        if (operationId == Guid.Empty || !Principal.FindAll("operation_id").Concat(Principal.FindAll("mining_unit"))
            .Any(claim => Guid.TryParse(claim.Value, out var id) && id == operationId))
            throw new FeedbackForbiddenException();
    }
}
