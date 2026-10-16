using System.Net.Mime;
using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Domain.Services;
using EdgeSynchronizationService.Interfaces.REST.Resources;
using EdgeSynchronizationService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace EdgeSynchronizationService.Interfaces.REST;

[ApiController]
[Route("api/v1/edge-sync")]
[Produces(MediaTypeNames.Application.Json)]
public class EdgeSynchronizationController(
    ISynchronizationCommandService commandService,
    ISynchronizationQueryService queryService) : ControllerBase
{
    [HttpPost("events")]
    public async Task<IActionResult> Enqueue(EnqueueSyncEventResource resource)
    {
        var result = await commandService.EnqueueAsync(
            SyncEventCommandFromResourceAssembler.ToCommand(resource));
        var response = SyncEventResource.FromEntity(result.Event);
        return result.AlreadyExists
            ? Ok(response)
            : CreatedAtAction(nameof(GetByVehicle), new { vehicleId = result.Event.VehicleId }, response);
    }

    [HttpPost("sync")]
    public async Task<IActionResult> Synchronize([FromQuery] string? vehicleId, [FromQuery] int batchSize = 100)
    {
        var events = await commandService.SynchronizeAsync(
            new SynchronizePendingEventsCommand(vehicleId, batchSize));
        return Ok(events.Select(SyncEventResource.FromEntity));
    }

    [HttpGet("vehicles/{vehicleId}/events")]
    public async Task<IActionResult> GetByVehicle(string vehicleId)
    {
        var events = await queryService.GetByVehicleAsync(vehicleId);
        return Ok(events.Select(SyncEventResource.FromEntity));
    }
}
