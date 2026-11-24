using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;
using PrivacyAuditService.Interfaces.REST.Transform;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/audit/incidents")]
[Produces(MediaTypeNames.Application.Json)]
public class IncidentHistoryController(IPrivacyAuditQueryService queryService) : ControllerBase
{
    /// <summary>
    /// Chronological traceability of an incident (US23) as seen by the audit log. Lives under the
    /// Privacy &amp; Audit prefix so it does not collide with Incident Management, which owns
    /// <c>/api/v1/incidents</c>.
    /// Steps: accesses recorded in the audit log for this incident id plus its opening and closure,
    /// received as Incident Management events (ADR-004).
    /// </summary>
    [HttpGet("{incidentId:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<IncidentHistoryEntryResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncidentHistory(Guid incidentId)
    {
        var history = await queryService.Handle(new GetIncidentHistoryQuery(incidentId));
        return Ok(history.Select(IncidentHistoryEntryResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
