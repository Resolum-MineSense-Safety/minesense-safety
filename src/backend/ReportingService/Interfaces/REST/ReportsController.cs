using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Domain.Services;
using ReportingService.Interfaces.REST.Resources;

namespace ReportingService.Interfaces.REST;

[ApiController]
[Route("api/v1/reports")]
[Produces(MediaTypeNames.Application.Json)]
public class ReportsController(
    IReportingCommandService commandService,
    IReportingQueryService queryService) : ControllerBase
{
    [HttpPost("safety")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult RequestSafetyReport([FromBody] RequestSafetyReportResource resource)
    {
        _ = commandService;
        _ = queryService;
        return StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "ReportingService scaffold (US22): safety report request is not implemented yet." });
    }

    [HttpGet("{reportJobId:guid}")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetReportJobById(Guid reportJobId) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "ReportingService scaffold (US22): report job query is not implemented yet." });

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetReportJobs(
        [FromQuery] string? siteId,
        [FromQuery] string? fleetId,
        [FromQuery] DateTimeOffset? from,
        [FromQuery] DateTimeOffset? to,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "ReportingService scaffold (US22): report jobs query is not implemented yet." });

    [HttpGet("{reportJobId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult DownloadReportArtifact(Guid reportJobId) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "ReportingService scaffold (US22): artifact download is not implemented yet." });
}
