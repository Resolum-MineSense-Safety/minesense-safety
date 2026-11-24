using System.Net.Mime;
using AnalyticsService.Domain.Services;
using AnalyticsService.Interfaces.REST.Resources;
using AnalyticsService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace AnalyticsService.Interfaces.REST;

/// <summary>Ingestion of fatigue alerts into the analytical model (until the Kafka consumer exists).</summary>
[ApiController]
[Route("api/v1/analytics/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class FatigueEventsController(IFatigueMetricCommandService fatigueMetricCommandService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(FatigueMetricResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RecordFatigueEvent([FromBody] RecordFatigueEventResource resource)
    {
        var command = RecordFatigueMetricCommandFromResourceAssembler.ToCommandFromResource(resource);
        var metric = await fatigueMetricCommandService.Handle(command);
        return StatusCode(StatusCodes.Status201Created,
            FatigueMetricResourceFromEntityAssembler.ToResourceFromEntity(metric));
    }
}
