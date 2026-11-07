using System.Net.Mime;
using FatigueDetectionService.Domain.Model.Queries;
using FatigueDetectionService.Domain.Services;
using FatigueDetectionService.Interfaces.REST.Resources;
using FatigueDetectionService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace FatigueDetectionService.Interfaces.REST;

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class FatigueAssessmentsController(
    IFatigueAssessmentCommandService fatigueAssessmentCommandService,
    IFatigueAssessmentQueryService fatigueAssessmentQueryService) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(typeof(FatigueAssessmentResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> AssessFatigue([FromBody] AssessFatigueResource resource)
    {
        var command = AssessFatigueCommandFromResourceAssembler.ToCommandFromResource(resource);
        var assessment = await fatigueAssessmentCommandService.Handle(command);
        var assessmentResource = FatigueAssessmentResourceFromEntityAssembler.ToResourceFromEntity(assessment);
        return CreatedAtAction(nameof(GetFatigueAssessmentById),
            new { fatigueAssessmentId = assessment.Id }, assessmentResource);
    }

    [HttpGet("{fatigueAssessmentId:guid}")]
    [ProducesResponseType(typeof(FatigueAssessmentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFatigueAssessmentById(Guid fatigueAssessmentId)
    {
        var assessment = await fatigueAssessmentQueryService.Handle(
            new GetFatigueAssessmentByIdQuery(fatigueAssessmentId));
        if (assessment is null) return NotFound();
        return Ok(FatigueAssessmentResourceFromEntityAssembler.ToResourceFromEntity(assessment));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FatigueAssessmentResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFatigueAssessmentsByOperatorId([FromQuery] Guid operatorId)
    {
        var assessments = await fatigueAssessmentQueryService.Handle(
            new GetFatigueAssessmentsByOperatorIdQuery(operatorId));
        return Ok(assessments.Select(FatigueAssessmentResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("latest")]
    [ProducesResponseType(typeof(FatigueAssessmentResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestFatigueAssessmentByOperatorId([FromQuery] Guid operatorId)
    {
        var assessment = await fatigueAssessmentQueryService.Handle(
            new GetLatestFatigueAssessmentByOperatorIdQuery(operatorId));
        if (assessment is null) return NotFound();
        return Ok(FatigueAssessmentResourceFromEntityAssembler.ToResourceFromEntity(assessment));
    }

    [HttpGet("sessions/{monitoringSessionId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<FatigueAssessmentResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetFatigueAssessmentsByMonitoringSessionId(Guid monitoringSessionId)
    {
        var assessments = await fatigueAssessmentQueryService.Handle(
            new GetFatigueAssessmentsByMonitoringSessionIdQuery(monitoringSessionId));
        return Ok(assessments.Select(FatigueAssessmentResourceFromEntityAssembler.ToResourceFromEntity));
    }
}
