using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Services;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/processing-purposes")]
[Produces(MediaTypeNames.Application.Json)]
public class ProcessingPurposesController(IPrivacyAuditQueryService queryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetProcessingPurposes()
    {
        _ = queryService;
        return StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PrivacyAuditService scaffold (US20): processing purposes catalog is not implemented yet." });
    }
}
