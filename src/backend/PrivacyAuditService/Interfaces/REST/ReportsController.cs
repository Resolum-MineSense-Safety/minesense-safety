using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using PrivacyAuditService.Domain.Model.Queries;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;
using PrivacyAuditService.Interfaces.REST.Transform;

namespace PrivacyAuditService.Interfaces.REST;

[ApiController]
[Route("api/v1/reports")]
[Produces(MediaTypeNames.Application.Json)]
public class ReportsController(
    IPrivacyAuditCommandService commandService,
    IPrivacyAuditQueryService queryService) : ControllerBase
{
    [HttpPost("safety")]
    [ProducesResponseType(typeof(ReportResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestSafetyReport([FromBody] RequestSafetyReportResource resource)
    {
        var command = RequestSafetyReportCommandFromResourceAssembler.ToCommandFromResource(resource);
        var report = await commandService.Handle(command);
        var reportResource = ReportResourceFromEntityAssembler.ToResourceFromEntity(report);
        return CreatedAtAction(nameof(GetReportById), new { reportId = report.Id }, reportResource);
    }

    [HttpGet("{reportId:guid}")]
    [ProducesResponseType(typeof(ReportResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportById(Guid reportId)
    {
        var report = await queryService.Handle(new GetReportByIdQuery(reportId));
        if (report is null) return NotFound();
        return Ok(ReportResourceFromEntityAssembler.ToResourceFromEntity(report));
    }
}
