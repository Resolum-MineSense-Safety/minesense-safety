using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using ReportingService.Domain.Model.Queries;
using ReportingService.Domain.Services;
using ReportingService.Interfaces.REST.Resources;
using ReportingService.Interfaces.REST.Transform;

namespace ReportingService.Interfaces.REST;

[ApiController]
[Route("api/v1/report-definitions")]
[Produces(MediaTypeNames.Application.Json)]
public class ReportDefinitionsController(
    IReportingCommandService commandService,
    IReportingQueryService queryService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ReportDefinitionResource>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetReportDefinitions([FromQuery] bool onlyActive = true)
    {
        var definitions = await queryService.Handle(new GetReportDefinitionsQuery(onlyActive));
        return Ok(definitions.Select(ReportDefinitionResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ReportDefinitionResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> DefineReportIndicator([FromBody] DefineReportIndicatorResource resource)
    {
        var command = DefineReportIndicatorCommandFromResourceAssembler.ToCommandFromResource(resource);
        var (definition, created) = await commandService.Handle(command);
        var definitionResource = ReportDefinitionResourceFromEntityAssembler.ToResourceFromEntity(definition);
        return created ? StatusCode(StatusCodes.Status201Created, definitionResource) : Conflict(definitionResource);
    }
}
