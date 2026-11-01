using System.Net.Mime;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Domain.Model.Queries;
using ReportingService.Domain.Services;
using ReportingService.Interfaces.REST.Resources;
using ReportingService.Interfaces.REST.Transform;
using MineSenseSafety.Shared.Domain.Model;

namespace ReportingService.Interfaces.REST;

[ApiController]
[Route("api/v1/reports")]
[Produces(MediaTypeNames.Application.Json)]
public class ReportsController(
    IReportingCommandService commandService,
    IReportingQueryService queryService,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost("safety")]
    [ProducesResponseType(typeof(ReportJobResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> RequestSafetyReport([FromBody] RequestSafetyReportResource resource)
    {
        var command = RequestReportGenerationCommandFromResourceAssembler.ToCommandFromResource(
            resource, AuthenticatedUserId());
        var job = await commandService.Handle(command);
        var jobResource = ReportJobResourceFromEntityAssembler.ToResourceFromEntity(job);
        return CreatedAtAction(nameof(GetReportJobById), new { reportJobId = job.Id }, jobResource);
    }

    [HttpGet("{reportJobId:guid}")]
    [ProducesResponseType(typeof(ReportJobResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetReportJobById(Guid reportJobId)
    {
        var job = await queryService.Handle(new GetReportJobByIdQuery(reportJobId));
        if (job is null) return NotFound();
        return Ok(ReportJobResourceFromEntityAssembler.ToResourceFromEntity(job));
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReportJobResource>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetReportJobs(
        [FromQuery] string? siteId,
        [FromQuery] string? fleetId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        if (page < 1 || pageSize < 1 || pageSize > 100)
            throw new DomainException("Page must be >= 1 and pageSize between 1 and 100.");

        var jobs = await queryService.Handle(new GetReportJobsQuery(siteId, fleetId, from, to, page, pageSize));
        return Ok(jobs.Select(ReportJobResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{reportJobId:guid}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> DownloadReportArtifact(Guid reportJobId)
    {
        var job = await queryService.Handle(new GetReportJobByIdQuery(reportJobId));
        if (job is null) return NotFound();

        var artifact = await queryService.FindArtifactByJobIdAsync(reportJobId);
        if (artifact is null)
            return Conflict(new { message = $"The report is not ready (status {job.Status})." });
        if (artifact.IsExpired(timeProvider.GetUtcNow()))
            return StatusCode(StatusCodes.Status410Gone, new { message = "The report artifact has expired." });

        Response.Headers["X-Checksum"] = artifact.Checksum;
        return File(artifact.Content.ToArray(), artifact.ContentType, artifact.FileName);
    }

    /// <summary>The <c>sub</c> claim of the caller's token, when the request is authenticated.</summary>
    private Guid? AuthenticatedUserId()
    {
        var subject = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(subject, out var userId) ? userId : null;
    }
}
