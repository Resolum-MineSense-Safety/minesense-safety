using System.Net.Mime;
using FleetMonitoringService.Domain.Model.Queries;
using FleetMonitoringService.Domain.Services;
using FleetMonitoringService.Interfaces.REST.Resources;
using FleetMonitoringService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace FleetMonitoringService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class FleetOperatorsController(
    IMonitoredOperatorCommandService monitoredOperatorCommandService,
    IMonitoredOperatorQueryService monitoredOperatorQueryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterMonitoredOperator([FromBody] RegisterMonitoredOperatorResource resource)
    {
        var command = RegisterMonitoredOperatorCommandFromResourceAssembler.ToCommandFromResource(resource);
        var monitoredOperator = await monitoredOperatorCommandService.Handle(command);
        var operatorResource = MonitoredOperatorResourceFromEntityAssembler.ToResourceFromEntity(monitoredOperator);
        return CreatedAtAction(nameof(GetMonitoredOperatorByOperatorId),
            new { operatorId = monitoredOperator.OperatorId }, operatorResource);
    }

    [HttpGet]
    public async Task<IActionResult> GetFleetStatus(
        [FromQuery] string? shift, [FromQuery] string? fleet,
        [FromQuery] string? location, [FromQuery] string? riskLevel)
    {
        var query = GetFleetStatusQueryFromParametersAssembler.ToQueryFromParameters(shift, fleet, location, riskLevel);
        var monitoredOperators = await monitoredOperatorQueryService.Handle(query);
        return Ok(monitoredOperators.Select(MonitoredOperatorResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{operatorId:guid}")]
    public async Task<IActionResult> GetMonitoredOperatorByOperatorId(Guid operatorId)
    {
        var monitoredOperator = await monitoredOperatorQueryService.Handle(
            new GetMonitoredOperatorByOperatorIdQuery(operatorId));
        if (monitoredOperator is null) return NotFound();
        return Ok(MonitoredOperatorResourceFromEntityAssembler.ToResourceFromEntity(monitoredOperator));
    }

    [HttpPut("{operatorId:guid}/risk-level")]
    public async Task<IActionResult> UpdateOperatorRiskLevel(
        Guid operatorId, [FromBody] UpdateOperatorRiskLevelResource resource)
    {
        var command = UpdateOperatorRiskLevelCommandFromResourceAssembler.ToCommandFromResource(operatorId, resource);
        var monitoredOperator = await monitoredOperatorCommandService.Handle(command);
        if (monitoredOperator is null) return NotFound();
        return Ok(MonitoredOperatorResourceFromEntityAssembler.ToResourceFromEntity(monitoredOperator));
    }

    [HttpPut("{operatorId:guid}/vehicle")]
    public async Task<IActionResult> ReassignOperatorVehicle(
        Guid operatorId, [FromBody] ReassignOperatorVehicleResource resource)
    {
        var command = ReassignOperatorVehicleCommandFromResourceAssembler.ToCommandFromResource(operatorId, resource);
        var monitoredOperator = await monitoredOperatorCommandService.Handle(command);
        if (monitoredOperator is null) return NotFound();
        return Ok(MonitoredOperatorResourceFromEntityAssembler.ToResourceFromEntity(monitoredOperator));
    }
}
