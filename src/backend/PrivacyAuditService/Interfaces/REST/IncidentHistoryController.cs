using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;
using PrivacyAuditService.Interfaces.REST.Transform;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/incidents")]
[Produces(MediaTypeNames.Application.Json)]
public class IncidentHistoryController(IPrivacyAuditQueryService queryService) : ControllerBase
{
    /// <summary>
    /// Chronological traceability of an incident (US23).
    /// Current slice: steps recorded in the access audit log for this incident id.
    /// Detection/alert/lifecycle steps arrive with Fase 3 (producer events).
    /// </summary>
    [HttpGet("{incidentId:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<IncidentHistoryEntryResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetIncidentHistory(Guid incidentId)
    {
        var history = await queryService.Handle(new GetIncidentHistoryQuery(incidentId));
        return Ok(history.Select(IncidentHistoryEntryResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
