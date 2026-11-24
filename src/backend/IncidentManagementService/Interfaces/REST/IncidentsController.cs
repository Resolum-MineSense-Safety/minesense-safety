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
    /// <summary>Receives an incident from an alert (US09). Repeated alerts return the same incident.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> OpenIncident([FromBody] OpenIncidentResource resource)
    {
        var command = OpenIncidentCommandFromResourceAssembler.ToCommandFromResource(resource);
        var incident = await incidentCommandService.Handle(command);
        var incidentResource = IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident);
        return CreatedAtAction(nameof(GetIncidentById), new { incidentId = incident.Id }, incidentResource);
    }

    /// <summary>Incident tray filtered by status (Pending, Assigned, Closed) and/or operator.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<IncidentResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetIncidents([FromQuery] string? status, [FromQuery] Guid? operatorId)
    {
        IncidentStatus? incidentStatus = null;
        if (status is not null)
        {
            if (!Enum.TryParse<IncidentStatus>(status, ignoreCase: true, out var parsed))
                throw new DomainException($"Unknown incident status '{status}'.");
            incidentStatus = parsed;
        }

        var incidents = await incidentQueryService.Handle(new GetIncidentsQuery(incidentStatus, operatorId));
        return Ok(incidents.Select(IncidentResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{incidentId:guid}")]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetIncidentById(Guid incidentId)
    {
        var incident = await incidentQueryService.Handle(new GetIncidentByIdQuery(incidentId));
        if (incident is null) return NotFound();
        return Ok(IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident));
    }

    /// <summary>TS-16: a supervisor takes ownership of a pending incident.</summary>
    [HttpPatch("{incidentId:guid}/assignment")]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> AssignIncident(Guid incidentId, [FromBody] AssignIncidentResource resource)
    {
        var incident = await incidentCommandService.Handle(new AssignIncidentCommand(incidentId, resource.AssigneeId));
        if (incident is null) return NotFound();
        return Ok(IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident));
    }

    /// <summary>TS-17: records a pause, contact, relief or other response.</summary>
    [HttpPost("{incidentId:guid}/actions")]
    [ProducesResponseType(typeof(IncidentActionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> RecordAction(Guid incidentId, [FromBody] RecordIncidentActionResource resource)
    {
        var command = RecordIncidentActionCommandFromResourceAssembler.ToCommandFromResource(incidentId, resource);
        var action = await incidentCommandService.Handle(command);
        if (action is null) return NotFound();
        return CreatedAtAction(nameof(GetIncidentById), new { incidentId },
            IncidentResourceFromEntityAssembler.ToResourceFromEntity(action));
    }

    /// <summary>TS-18: closes an incident that has at least one documented action.</summary>
    [HttpPatch("{incidentId:guid}/closure")]
    [ProducesResponseType(typeof(IncidentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CloseIncident(Guid incidentId, [FromBody] CloseIncidentResource resource)
    {
        var incident = await incidentCommandService.Handle(
            new CloseIncidentCommand(incidentId, resource.Resolution, resource.ClosedAt));
        if (incident is null) return NotFound();
        return Ok(IncidentResourceFromEntityAssembler.ToResourceFromEntity(incident));
    }

    /// <summary>TS-19: escalation level and notified roles of an incident.</summary>
    [HttpGet("{incidentId:guid}/escalation")]
    [ProducesResponseType(typeof(IncidentEscalationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetEscalation(Guid incidentId)
    {
        var incident = await incidentQueryService.Handle(new GetIncidentByIdQuery(incidentId));
        if (incident is null) return NotFound();
        return Ok(IncidentEscalationResourceFromEntityAssembler.ToResourceFromEntity(incident));
    }

    /// <summary>TS-27: chronological history (reception, assignment, actions, escalation, closure).</summary>
    [HttpGet("{incidentId:guid}/history")]
    [ProducesResponseType(typeof(IEnumerable<IncidentEventResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetHistory(Guid incidentId)
    {
        var incident = await incidentQueryService.Handle(new GetIncidentByIdQuery(incidentId));
        if (incident is null) return NotFound();
        return Ok(incident.History
            .OrderBy(incidentEvent => incidentEvent.OccurredAt)
            .Select(IncidentEventResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>Escalates every unattended critical incident whose deadline has passed.</summary>
    [HttpPost("overdue-escalations")]
    [ProducesResponseType(typeof(IEnumerable<IncidentResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EscalateOverdueIncidents()
    {
        var incidents = await incidentCommandService.Handle(new EscalateOverdueIncidentsCommand());
        return Ok(incidents.Select(IncidentResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
