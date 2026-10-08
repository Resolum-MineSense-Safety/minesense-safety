using System.Net.Mime;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.Queries;
using AlertService.Domain.Services;
using AlertService.Interfaces.REST.Resources;
using AlertService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace AlertService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class AlertsController(
    IAlertCommandService alertCommandService,
    IAlertQueryService alertQueryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> IssueAlert([FromBody] IssueAlertResource resource)
    {
        var command = IssueAlertCommandFromResourceAssembler.ToCommandFromResource(resource);
        var alert = await alertCommandService.Handle(command);
        var alertResource = AlertResourceFromEntityAssembler.ToResourceFromEntity(alert);
        return CreatedAtAction(nameof(GetAlertById), new { alertId = alert.Id }, alertResource);
    }

    [HttpGet("{alertId:guid}")]
    public async Task<IActionResult> GetAlertById(Guid alertId)
    {
        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(alertId));
        if (alert is null) return NotFound();
        return Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert));
    }

    [HttpGet]
    public async Task<IActionResult> GetAlertsByOperatorId([FromQuery] Guid operatorId)
    {
        var alerts = await alertQueryService.Handle(new GetAlertsByOperatorIdQuery(operatorId));
        return Ok(alerts.Select(AlertResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPost("{alertId:guid}/acknowledgements")]
    public async Task<IActionResult> AcknowledgeAlert(Guid alertId)
    {
        var alert = await alertCommandService.Handle(new AcknowledgeAlertCommand(alertId));
        if (alert is null) return NotFound();
        return Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert));
    }

    [HttpPost("{alertId:guid}/escalations")]
    public async Task<IActionResult> EscalateAlert(Guid alertId)
    {
        var alert = await alertCommandService.Handle(new EscalateAlertCommand(alertId));
        if (alert is null) return NotFound();
        return Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert));
    }
}
