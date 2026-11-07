using System.Net.Mime;
using AlertService.Domain.Model.Commands;
using AlertService.Domain.Model.Queries;
using AlertService.Domain.Model.ValueObjects;
using AlertService.Domain.Services;
using AlertService.Interfaces.REST.Resources;
using AlertService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using MineSenseSafety.Shared.Domain.Model;

namespace AlertService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class AlertsController(
    IAlertCommandService alertCommandService,
    IAlertQueryService alertQueryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> IssueAlert([FromBody] IssueAlertResource resource)
    {
        var command = IssueAlertCommandFromResourceAssembler.ToCommandFromResource(resource);
        var alert = await alertCommandService.Handle(command);
        var alertResource = AlertResourceFromEntityAssembler.ToResourceFromEntity(alert);
        return CreatedAtAction(nameof(GetAlertById), new { alertId = alert.Id }, alertResource);
    }

    [HttpGet("{alertId:guid}")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAlertById(Guid alertId)
    {
        var alert = await alertQueryService.Handle(new GetAlertByIdQuery(alertId));
        if (alert is null) return NotFound();
        return Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AlertResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAlertsByOperatorId([FromQuery] Guid operatorId)
    {
        var alerts = await alertQueryService.Handle(new GetAlertsByOperatorIdQuery(operatorId));
        return Ok(alerts.Select(AlertResourceFromEntityAssembler.ToResourceFromEntity));
    }

    /// <summary>Supervisor queue: alerts in a given status (Issued, Acknowledged or Escalated).</summary>
    [HttpGet("status/{status}")]
    [ProducesResponseType(typeof(IEnumerable<AlertResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetAlertsByStatus(string status)
    {
        if (!Enum.TryParse<AlertStatus>(status, ignoreCase: true, out var alertStatus))
            throw new DomainException($"Unknown alert status '{status}'.");

        var alerts = await alertQueryService.Handle(new GetAlertsByStatusQuery(alertStatus));
        return Ok(alerts.Select(AlertResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPost("{alertId:guid}/acknowledgements")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AcknowledgeAlert(Guid alertId)
    {
        var alert = await alertCommandService.Handle(new AcknowledgeAlertCommand(alertId));
        if (alert is null) return NotFound();
        return Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert));
    }

    [HttpPost("{alertId:guid}/escalations")]
    [ProducesResponseType(typeof(AlertResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EscalateAlert(Guid alertId)
    {
        var alert = await alertCommandService.Handle(new EscalateAlertCommand(alertId));
        if (alert is null) return NotFound();
        return Ok(AlertResourceFromEntityAssembler.ToResourceFromEntity(alert));
    }

    /// <summary>Escalates every issued alert whose acknowledgement deadline has passed.</summary>
    [HttpPost("overdue-escalations")]
    [ProducesResponseType(typeof(IEnumerable<AlertResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> EscalateOverdueAlerts()
    {
        var alerts = await alertCommandService.Handle(new EscalateOverdueAlertsCommand());
        return Ok(alerts.Select(AlertResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
