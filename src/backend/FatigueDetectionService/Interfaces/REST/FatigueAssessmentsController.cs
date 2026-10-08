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
    public async Task<IActionResult> AssessFatigue([FromBody] AssessFatigueResource resource)
    {
        var command = AssessFatigueCommandFromResourceAssembler.ToCommandFromResource(resource);
        var assessment = await fatigueAssessmentCommandService.Handle(command);
        var assessmentResource = FatigueAssessmentResourceFromEntityAssembler.ToResourceFromEntity(assessment);
        return CreatedAtAction(nameof(GetFatigueAssessmentById),
            new { fatigueAssessmentId = assessment.Id }, assessmentResource);
    }

    [HttpGet("{fatigueAssessmentId:guid}")]
    public async Task<IActionResult> GetFatigueAssessmentById(Guid fatigueAssessmentId)
    {
        var assessment = await fatigueAssessmentQueryService.Handle(
            new GetFatigueAssessmentByIdQuery(fatigueAssessmentId));
        if (assessment is null) return NotFound();
        return Ok(FatigueAssessmentResourceFromEntityAssembler.ToResourceFromEntity(assessment));
    }

    [HttpGet]
    public async Task<IActionResult> GetFatigueAssessmentsByOperatorId([FromQuery] Guid operatorId)
    {
        var assessments = await fatigueAssessmentQueryService.Handle(
            new GetFatigueAssessmentsByOperatorIdQuery(operatorId));
        return Ok(assessments.Select(FatigueAssessmentResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestFatigueAssessmentByOperatorId([FromQuery] Guid operatorId)
    {
        var assessment = await fatigueAssessmentQueryService.Handle(
            new GetLatestFatigueAssessmentByOperatorIdQuery(operatorId));
        if (assessment is null) return NotFound();
        return Ok(FatigueAssessmentResourceFromEntityAssembler.ToResourceFromEntity(assessment));
    }
}
