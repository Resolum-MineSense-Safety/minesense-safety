using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Services;
using OperationsService.Interfaces.REST.Resources;
using OperationsService.Interfaces.REST.Transform;

namespace OperationsService.Interfaces.REST;

// Sprint 1 · T14 (Joseph Huamani): CRUD endpoints for fleets (US17). Remaining: update/deactivate and admin views in src/frontend.
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class FleetsController(
    IFleetCommandService fleetCommandService,
    IFleetQueryService fleetQueryService,
    IVehicleCommandService vehicleCommandService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterFleet([FromBody] RegisterFleetResource resource)
    {
        var command = RegisterFleetCommandFromResourceAssembler.ToCommandFromResource(resource);
        var fleet = await fleetCommandService.Handle(command);
        var fleetResource = FleetResourceFromEntityAssembler.ToResourceFromEntity(fleet);
        return CreatedAtAction(nameof(GetFleetById), new { fleetId = fleet.Id }, fleetResource);
    }

    /// <summary>TS-06: registers a vehicle inside the fleet (404 unknown fleet, 409 duplicated code).</summary>
    [HttpPost("{fleetId:guid}/vehicles")]
    public async Task<IActionResult> RegisterFleetVehicle(Guid fleetId, [FromBody] RegisterFleetVehicleResource resource)
    {
        var command = RegisterVehicleCommandFromResourceAssembler.ToCommandFromResource(fleetId, resource);
        var vehicle = await vehicleCommandService.Handle(command);
        var vehicleResource = VehicleResourceFromEntityAssembler.ToResourceFromEntity(vehicle);
        return CreatedAtAction(nameof(VehiclesController.GetVehicleById), "Vehicles",
            new { vehicleId = vehicle.Id }, vehicleResource);
    }

    [HttpGet("{fleetId:guid}")]
    public async Task<IActionResult> GetFleetById(Guid fleetId)
    {
        var fleet = await fleetQueryService.Handle(new GetFleetByIdQuery(fleetId));
        if (fleet is null) return NotFound();
        return Ok(FleetResourceFromEntityAssembler.ToResourceFromEntity(fleet));
    }

    [HttpGet]
    public async Task<IActionResult> GetFleets([FromQuery] Guid? miningUnitId)
    {
        var fleets = await fleetQueryService.Handle(new GetFleetsByMiningUnitQuery(miningUnitId));
        return Ok(fleets.Select(FleetResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
