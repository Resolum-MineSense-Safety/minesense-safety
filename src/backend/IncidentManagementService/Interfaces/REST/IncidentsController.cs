using System.Net.Mime;
using IncidentManagementService.Domain.Model.Commands;
using IncidentManagementService.Domain.Model.Queries;
using IncidentManagementService.Domain.Model.ValueObjects;
using IncidentManagementService.Domain.Services;
using IncidentManagementService.Interfaces.REST.Resources;
using IncidentManagementService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using MineSenseSafety.Shared.Domain.Model;

namespace IncidentManagementService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class IncidentsController(
    IIncidentCommandService incidentCommandService,
    IIncidentQueryService incidentQueryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> OpenIncident([FromBody] OpenIncidentResource resource)
    {
        var command = OpenIncidentCommandFromResourceAssembler.ToCommandFromResource(resource);
        var incident = await incidentCommandService.Handle(command);
        var incidentResource = IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident);
        return CreatedAtAction(nameof(GetIncidentById), new { incidentId = incident.Id }, incidentResource);
    }

    [HttpGet]
    public async Task<IActionResult> GetIncidentsByStatus([FromQuery] string? status)
    {
        IncidentStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<IncidentStatus>(status, ignoreCase: true, out var value))
                throw new DomainException($"Unknown incident status '{status}'.");
            parsedStatus = value;
        }

        var incidents = await incidentQueryService.Handle(new GetIncidentsByStatusQuery(parsedStatus));
        return Ok(incidents.Select(IncidentResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{incidentId:guid}")]
    public async Task<IActionResult> GetIncidentById(Guid incidentId)
    {
        var incident = await incidentQueryService.Handle(new GetIncidentByIdQuery(incidentId));
        if (incident is null) return NotFound();
        return Ok(IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident));
    }

    [HttpPost("{incidentId:guid}/assignment")]
    public async Task<IActionResult> AssignIncident(Guid incidentId, [FromBody] AssignIncidentResource resource)
    {
        var command = AssignIncidentCommandFromResourceAssembler.ToCommandFromResource(incidentId, resource);
        var incident = await incidentCommandService.Handle(command);
        if (incident is null) return NotFound();
        return Ok(IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident));
    }

    [HttpPost("{incidentId:guid}/actions")]
    public async Task<IActionResult> RegisterIncidentAction(
        Guid incidentId, [FromBody] RegisterIncidentActionResource resource)
    {
        var command = RegisterIncidentActionCommandFromResourceAssembler.ToCommandFromResource(incidentId, resource);
        var incident = await incidentCommandService.Handle(command);
        if (incident is null) return NotFound();
        return Ok(IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident));
    }

    [HttpPost("{incidentId:guid}/escalation")]
    public async Task<IActionResult> EscalateIncident(Guid incidentId)
    {
        var incident = await incidentCommandService.Handle(new EscalateIncidentCommand(incidentId));
        if (incident is null) return NotFound();
        return Ok(IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident));
    }

    [HttpPost("{incidentId:guid}/closure")]
    public async Task<IActionResult> CloseIncident(Guid incidentId, [FromBody] CloseIncidentResource resource)
    {
        var command = CloseIncidentCommandFromResourceAssembler.ToCommandFromResource(incidentId, resource);
        var incident = await incidentCommandService.Handle(command);
        if (incident is null) return NotFound();
        return Ok(IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident));
    }
}
