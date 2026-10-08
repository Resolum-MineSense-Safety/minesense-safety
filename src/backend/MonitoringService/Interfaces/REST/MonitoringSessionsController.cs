using System.Net.Mime;
using MonitoringService.Domain.Model.Commands;
using MonitoringService.Domain.Model.Queries;
using MonitoringService.Domain.Services;
using MonitoringService.Interfaces.REST.Resources;
using MonitoringService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace MonitoringService.Interfaces.REST;

// Sprint 1 · T05 (Ian Santisteban): Edge gateway extension point — the gateway reports device signals through
// POST {id}/signal-losses and POST {id}/signal-restorations.
// TODO(T05): ingest signal availability from the Edge gateway over MQTT and map each message to
// ReportSignalLostCommand / ReportSignalRestoredCommand (no MQTT client in Sprint 1).
// Sprint 1 · T09 (Jhosep Argomedo): the operator dashboard (src/frontend) consumes GET current?operatorId=.
// Remaining: status indicator in the frontend.
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class MonitoringSessionsController(
    IMonitoringSessionCommandService sessionCommandService,
    IMonitoringSessionQueryService sessionQueryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> StartMonitoring([FromBody] StartMonitoringResource resource)
    {
        var command = StartMonitoringCommandFromResourceAssembler.ToCommandFromResource(resource);
        var session = await sessionCommandService.Handle(command);
        var sessionResource = MonitoringSessionResourceFromEntityAssembler.ToResourceFromEntity(session);
        return CreatedAtAction(nameof(GetSessionById), new { sessionId = session.Id }, sessionResource);
    }

    [HttpGet]
    public async Task<IActionResult> GetSessions([FromQuery] string? status)
    {
        var query = new GetSessionsQuery(SignalTypeFromResourceAssembler.ToMonitoringStatus(status));
        var sessions = await sessionQueryService.Handle(query);
        return Ok(sessions.Select(MonitoringSessionResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{sessionId:guid}")]
    public async Task<IActionResult> GetSessionById(Guid sessionId)
    {
        var session = await sessionQueryService.Handle(new GetSessionByIdQuery(sessionId));
        if (session is null) return NotFound();
        return Ok(MonitoringSessionResourceFromEntityAssembler.ToResourceFromEntity(session));
    }

    // Sprint 1 · T08 (Renato Calvo): current monitoring status of an operator (US02).
    [HttpGet("current")]
    public async Task<IActionResult> GetCurrentStatus([FromQuery] Guid operatorId)
    {
        var session = await sessionQueryService.Handle(new GetCurrentStatusByOperatorQuery(operatorId));
        if (session is null) return NotFound();
        return Ok(MonitoringStatusResourceFromEntityAssembler.ToResourceFromEntity(session));
    }

    [HttpPost("{sessionId:guid}/signal-losses")]
    public async Task<IActionResult> ReportSignalLost(Guid sessionId, [FromBody] SignalReportResource resource)
    {
        var signal = SignalTypeFromResourceAssembler.ToSignalType(resource.Signal);
        var session = await sessionCommandService.Handle(new ReportSignalLostCommand(sessionId, signal));
        if (session is null) return NotFound();
        return Ok(MonitoringSessionResourceFromEntityAssembler.ToResourceFromEntity(session));
    }

    [HttpPost("{sessionId:guid}/signal-restorations")]
    public async Task<IActionResult> ReportSignalRestored(Guid sessionId, [FromBody] SignalReportResource resource)
    {
        var signal = SignalTypeFromResourceAssembler.ToSignalType(resource.Signal);
        var session = await sessionCommandService.Handle(new ReportSignalRestoredCommand(sessionId, signal));
        if (session is null) return NotFound();
        return Ok(MonitoringSessionResourceFromEntityAssembler.ToResourceFromEntity(session));
    }

    [HttpPost("{sessionId:guid}/stop")]
    public async Task<IActionResult> StopMonitoring(Guid sessionId)
    {
        var session = await sessionCommandService.Handle(new StopMonitoringCommand(sessionId));
        if (session is null) return NotFound();
        return Ok(MonitoringSessionResourceFromEntityAssembler.ToResourceFromEntity(session));
    }
}
