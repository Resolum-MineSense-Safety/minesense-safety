using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;
using PrivacyAuditService.Interfaces.REST.Transform;
using MineSenseSafety.Shared.Domain.Model;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/access-log")]
[Produces(MediaTypeNames.Application.Json)]
public class AccessLogController(
    IPrivacyAuditCommandService commandService,
    IPrivacyAuditQueryService queryService) : ControllerBase
{
    [HttpPost("entries")]
    [ProducesResponseType(typeof(AccessAuditEntryResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterAccessAttempt([FromBody] RegisterAccessAttemptResource resource)
    {
        var command = RegisterAccessAttemptCommandFromResourceAssembler.ToCommandFromResource(resource);
        var entry = await commandService.Handle(command);
        return StatusCode(StatusCodes.Status201Created,
            AccessAuditEntryResourceFromEntityAssembler.ToResourceFromEntity(entry));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AccessAuditEntryResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetAccessLog(
        [FromQuery] Guid? operatorId,
        [FromQuery] Guid? userId,
        [FromQuery] string? resourceType,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] string? decision,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            throw new DomainException("Page must be >= 1 and pageSize between 1 and 100.");

        AccessDecision? parsedDecision = null;
        if (!string.IsNullOrWhiteSpace(decision))
        {
            if (!Enum.TryParse<AccessDecision>(decision, ignoreCase: true, out var value))
                throw new DomainException($"Unknown access decision '{decision}'.");
            parsedDecision = value;
        }

        var entries = await queryService.Handle(new GetAccessLogQuery(operatorId, userId,
            resourceType, from, to, parsedDecision, page, pageSize));
        return Ok(entries.Select(AccessAuditEntryResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
