using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/reports")]
[Produces(MediaTypeNames.Application.Json)]
public class ReportsController(
    IPrivacyAuditCommandService commandService,
    IPrivacyAuditQueryService queryService) : ControllerBase
{
    [HttpPost("safety")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult RequestSafetyReport([FromBody] RequestSafetyReportResource resource)
    {
        _ = commandService;
        _ = queryService;
        return StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PrivacyAuditService scaffold (US22): safety report request is not implemented yet." });
    }

    [HttpGet("{reportId:guid}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetReportById(Guid reportId) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PrivacyAuditService scaffold (US22): report query is not implemented yet." });
}
