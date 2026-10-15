using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/access-log")]
[Produces(MediaTypeNames.Application.Json)]
public class AccessLogController(
    IPrivacyAuditCommandService commandService,
    IPrivacyAuditQueryService queryService) : ControllerBase
{
    [HttpPost("entries")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult RegisterAccessAttempt([FromBody] RegisterAccessAttemptResource resource)
    {
        _ = commandService;
        _ = queryService;
        return StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PrivacyAuditService scaffold (US21): access attempt registration is not implemented yet." });
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetAccessLog(
        [FromQuery] Guid? operatorId,
        [FromQuery] Guid? userId,
        [FromQuery] string? resourceType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? decision,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PrivacyAuditService scaffold (US21): access log query is not implemented yet." });
}
