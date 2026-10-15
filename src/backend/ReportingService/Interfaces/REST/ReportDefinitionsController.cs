using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Domain.Services;
using ReportingService.Interfaces.REST.Resources;

namespace ReportingService.Interfaces.REST;

[ApiController]
[Route("api/v1/report-definitions")]
[Produces(MediaTypeNames.Application.Json)]
public class ReportDefinitionsController(
    IReportingCommandService commandService,
    IReportingQueryService queryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult GetReportDefinitions([FromQuery] bool onlyActive = true)
    {
        _ = commandService;
        _ = queryService;
        return StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "ReportingService scaffold (US22): definitions query is not implemented yet." });
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status501NotImplemented)]
    public IActionResult DefineReportIndicator([FromBody] DefineReportIndicatorResource resource) =>
        StatusCode(StatusCodes.Status501NotImplemented,
            new { message = "ReportingService scaffold (US22): indicator definition is not implemented yet." });
}
