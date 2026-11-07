using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Services;
using OperationsService.Interfaces.REST.Resources;
using OperationsService.Interfaces.REST.Transform;

namespace OperationsService.Interfaces.REST;

// Sprint 1 · T14 (Joseph Huamani): CRUD endpoints for vehicles (US17). Remaining: decommission and admin views in src/frontend.
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class VehiclesController(
    IVehicleCommandService vehicleCommandService,
    IVehicleQueryService vehicleQueryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterVehicle([FromBody] RegisterVehicleResource resource)
    {
        var command = RegisterVehicleCommandFromResourceAssembler.ToCommandFromResource(resource);
        var vehicle = await vehicleCommandService.Handle(command);
        var vehicleResource = VehicleResourceFromEntityAssembler.ToResourceFromEntity(vehicle);
        return CreatedAtAction(nameof(GetVehicleById), new { vehicleId = vehicle.Id }, vehicleResource);
    }

    [HttpGet]
    public async Task<IActionResult> GetVehicles([FromQuery] Guid? fleetId)
    {
        var vehicles = await vehicleQueryService.Handle(new GetVehiclesQuery(fleetId));
        return Ok(vehicles.Select(VehicleResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{vehicleId:guid}")]
    public async Task<IActionResult> GetVehicleById(Guid vehicleId)
    {
        var vehicle = await vehicleQueryService.Handle(new GetVehicleByIdQuery(vehicleId));
        if (vehicle is null) return NotFound();
        return Ok(VehicleResourceFromEntityAssembler.ToResourceFromEntity(vehicle));
    }

    [HttpPut("{vehicleId:guid}")]
    public async Task<IActionResult> UpdateVehicle(Guid vehicleId, [FromBody] UpdateVehicleResource resource)
    {
        var command = UpdateVehicleCommandFromResourceAssembler.ToCommandFromResource(vehicleId, resource);
        var vehicle = await vehicleCommandService.Handle(command);
        if (vehicle is null) return NotFound();
        return Ok(VehicleResourceFromEntityAssembler.ToResourceFromEntity(vehicle));
    }
}
