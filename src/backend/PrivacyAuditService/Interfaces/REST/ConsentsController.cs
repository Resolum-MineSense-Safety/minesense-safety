using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;
using PrivacyAuditService.Interfaces.REST.Transform;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/consents")]
[Produces(MediaTypeNames.Application.Json)]
public class ConsentsController(
    IPrivacyAuditCommandService commandService,
    IPrivacyAuditQueryService queryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(ConsentResource), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ConsentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterConsent([FromBody] RegisterConsentResource resource)
    {
        var command = RegisterConsentCommandFromResourceAssembler.ToCommandFromResource(resource);
        var (consent, created) = await commandService.Handle(command);
        var consentResource = ConsentResourceFromEntityAssembler.ToResourceFromEntity(consent);
        return created
            ? CreatedAtAction(nameof(GetCurrentConsent), new { operatorId = consent.OperatorId }, consentResource)
            : Ok(consentResource);
    }

    [HttpGet("operator/{operatorId:guid}/current")]
    [ProducesResponseType(typeof(ConsentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentConsent(Guid operatorId)
    {
        var consent = await queryService.Handle(new GetCurrentConsentByOperatorIdQuery(operatorId));
        if (consent is null) return NotFound();
        return Ok(ConsentResourceFromEntityAssembler.ToResourceFromEntity(consent));
    }

    [HttpGet("operator/{operatorId:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<ConsentResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConsentHistory(Guid operatorId)
    {
        var history = await queryService.Handle(new GetConsentHistoryByOperatorIdQuery(operatorId));
        return Ok(history.Select(ConsentResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
