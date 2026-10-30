using System.Net.Mime;
using FleetMonitoringService.Application.Internal.Configuration;
using FleetMonitoringService.Domain.Model.Aggregates;
using FleetMonitoringService.Domain.Model.Queries;
using FleetMonitoringService.Domain.Services;
using FleetMonitoringService.Interfaces.REST.Resources;
using FleetMonitoringService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace FleetMonitoringService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class FleetOperatorsController(
    IMonitoredOperatorCommandService monitoredOperatorCommandService,
    IMonitoredOperatorQueryService monitoredOperatorQueryService,
    TimeProvider timeProvider,
    IOptions<FleetMonitoringOptions> options) : ControllerBase
{
    private readonly TimeSpan _staleAfter = options.Value.StaleAfter;

    [HttpPost]
    public async Task<IActionResult> RegisterMonitoredOperator([FromBody] RegisterMonitoredOperatorResource resource)
    {
        var command = RegisterMonitoredOperatorCommandFromResourceAssembler.ToCommandFromResource(resource);
        var monitoredOperator = await monitoredOperatorCommandService.Handle(command);
        var operatorResource = ToResource(monitoredOperator);
        return CreatedAtAction(nameof(GetMonitoredOperatorByOperatorId),
            new { operatorId = monitoredOperator.OperatorId }, operatorResource);
    }

    [HttpGet]
    public async Task<IActionResult> GetFleetStatus(
        [FromQuery] string? shift, [FromQuery] string? fleet,
        [FromQuery] string? location, [FromQuery] string? riskLevel,
        [FromQuery] Guid? siteId, [FromQuery] Guid? miningUnitId, [FromQuery] Guid? fleetId)
        => await QueryFleetStatus(shift, fleet, location, riskLevel, siteId, miningUnitId, fleetId);

    [HttpGet("/api/v1/fleet/status")]
    public async Task<IActionResult> GetFleetStatusByDocumentedRoute(
        [FromQuery] string? shift, [FromQuery] string? fleet,
        [FromQuery] string? location, [FromQuery] string? riskLevel,
        [FromQuery] Guid? siteId, [FromQuery] Guid? miningUnitId, [FromQuery] Guid? fleetId)
        => await QueryFleetStatus(shift, fleet, location, riskLevel, siteId, miningUnitId, fleetId);

    [HttpGet("/api/v1/fleet-monitoring")]
    public async Task<IActionResult> GetFleetStatusByTechnicalStoryRoute(
        [FromQuery] string? shift, [FromQuery] string? fleet,
        [FromQuery] string? location, [FromQuery] string? riskLevel,
        [FromQuery] Guid? siteId, [FromQuery] Guid? miningUnitId, [FromQuery] Guid? fleetId)
        => await QueryFleetStatus(shift, fleet, location, riskLevel, siteId, miningUnitId, fleetId);

    private async Task<IActionResult> QueryFleetStatus(
        string? shift, string? fleet, string? location, string? riskLevel,
        Guid? siteId, Guid? miningUnitId, Guid? fleetId)
    {
        var query = GetFleetStatusQueryFromParametersAssembler.ToQueryFromParameters(
            shift, fleet, location, riskLevel, siteId, miningUnitId, fleetId);
        var monitoredOperators = await monitoredOperatorQueryService.Handle(query);
        return Ok(monitoredOperators.Select(ToResource));
    }

    [HttpGet("{operatorId:guid}")]
    public async Task<IActionResult> GetMonitoredOperatorByOperatorId(Guid operatorId)
    {
        var monitoredOperator = await monitoredOperatorQueryService.Handle(
            new GetMonitoredOperatorByOperatorIdQuery(operatorId));
        if (monitoredOperator is null) return NotFound();
        return Ok(ToResource(monitoredOperator));
    }

    [HttpGet("/api/v1/operators/{operatorId:guid}/status")]
    public Task<IActionResult> GetOperatorStatus(Guid operatorId) =>
        GetMonitoredOperatorByOperatorId(operatorId);

    [HttpPut("{operatorId:guid}/risk-level")]
    public async Task<IActionResult> UpdateOperatorRiskLevel(
        Guid operatorId, [FromBody] UpdateOperatorRiskLevelResource resource)
    {
        var command = UpdateOperatorRiskLevelCommandFromResourceAssembler.ToCommandFromResource(operatorId, resource);
        var monitoredOperator = await monitoredOperatorCommandService.Handle(command);
        if (monitoredOperator is null) return NotFound();
        return Ok(ToResource(monitoredOperator));
    }

    [HttpPut("{operatorId:guid}/vehicle")]
    public async Task<IActionResult> ReassignOperatorVehicle(
        Guid operatorId, [FromBody] ReassignOperatorVehicleResource resource)
    {
        var command = ReassignOperatorVehicleCommandFromResourceAssembler.ToCommandFromResource(operatorId, resource);
        var monitoredOperator = await monitoredOperatorCommandService.Handle(command);
        if (monitoredOperator is null) return NotFound();
        return Ok(ToResource(monitoredOperator));
    }

    private MonitoredOperatorResource ToResource(MonitoredOperator monitoredOperator) =>
        MonitoredOperatorResourceFromEntityAssembler.ToResourceFromEntity(
            monitoredOperator, timeProvider.GetUtcNow(), _staleAfter);
}
