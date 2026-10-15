using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/consents")]
[Produces(MediaTypeNames.Application.Json)]
public class ConsentsController(
    IPrivacyAuditCommandService commandService,
    IPrivacyAuditQueryService queryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult RegisterConsent([FromBody] RegisterConsentResource resource)
    {
        _ = commandService;
        _ = queryService;
        return StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PrivacyAuditService scaffold (US20): consent registration is not implemented yet." });
    }

    [HttpGet("operator/{operatorId:guid}/current")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetCurrentConsent(Guid operatorId) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PrivacyAuditService scaffold (US20): current consent query is not implemented yet." });

    [HttpGet("operator/{operatorId:guid}/history")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetConsentHistory(Guid operatorId) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "PrivacyAuditService scaffold (US20): consent history query is not implemented yet." });
}
