using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Services;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/incidents")]
[Produces(MediaTypeNames.Application.Json)]
public class IncidentHistoryController(IPrivacyAuditQueryService queryService) : ControllerBase
{
    [HttpGet("{incidentId:guid}/history")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetIncidentHistory(Guid incidentId)
    {
        _ = queryService;
        return StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PrivacyAuditService scaffold (US23): incident history query is not implemented yet." });
    }
}
