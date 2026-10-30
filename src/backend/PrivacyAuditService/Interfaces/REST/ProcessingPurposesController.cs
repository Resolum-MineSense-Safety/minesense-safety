using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Transform;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/processing-purposes")]
[Produces(MediaTypeNames.Application.Json)]
public class ProcessingPurposesController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Resources.ProcessingPurposesResource), StatusCodes.Status200OK)]
    public IActionResult GetProcessingPurposes() =>
        Ok(ProcessingPurposesResourceFromEntityAssembler.ToResource());
}
