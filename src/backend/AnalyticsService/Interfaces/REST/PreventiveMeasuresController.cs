using System.Net.Mime;
using AnalyticsService.Domain.Model.Queries;
using AnalyticsService.Domain.Services;
using AnalyticsService.Interfaces.REST.Resources;
using AnalyticsService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace AnalyticsService.Interfaces.REST;

[ApiController]
[Route("api/v1/analytics/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PreventiveMeasuresController(
    IPreventiveMeasureCommandService preventiveMeasureCommandService,
    IAnalyticsQueryService analyticsQueryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(PreventiveMeasureResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RegisterPreventiveMeasure([FromBody] RegisterPreventiveMeasureResource resource)
    {
        var command = RegisterPreventiveMeasureCommandFromResourceAssembler.ToCommandFromResource(resource);
        var measure = await preventiveMeasureCommandService.Handle(command);
        return CreatedAtAction(nameof(GetPreventiveMeasureById), new { measureId = measure.Id },
            PreventiveMeasureResourceFromEntityAssembler.ToResourceFromEntity(measure));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PreventiveMeasureResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllPreventiveMeasures()
    {
        var measures = await analyticsQueryService.Handle(new GetAllPreventiveMeasuresQuery());
        return Ok(measures.Select(PreventiveMeasureResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{measureId:guid}")]
    [ProducesResponseType(typeof(PreventiveMeasureResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPreventiveMeasureById(Guid measureId)
    {
        var measure = await analyticsQueryService.Handle(new GetPreventiveMeasureByIdQuery(measureId));
        if (measure is null) return NotFound();
        return Ok(PreventiveMeasureResourceFromEntityAssembler.ToResourceFromEntity(measure));
    }

    /// <summary>US25: indicators before and after the measure over equal windows (30 days by default).</summary>
    [HttpGet("{measureId:guid}/evaluation")]
    [ProducesResponseType(typeof(MeasureEvaluationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> EvaluatePreventiveMeasure(Guid measureId, [FromQuery] int windowDays = 30)
    {
        var evaluation = await analyticsQueryService.Handle(new EvaluatePreventiveMeasureQuery(measureId, windowDays));
        if (evaluation is null) return NotFound();
        return Ok(MeasureEvaluationResourceFromReadModelAssembler.ToResourceFromReadModel(evaluation));
    }
}
