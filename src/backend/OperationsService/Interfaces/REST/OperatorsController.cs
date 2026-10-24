using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Services;
using OperationsService.Interfaces.REST.Resources;
using OperationsService.Interfaces.REST.Transform;

namespace OperationsService.Interfaces.REST;

// TS-07: operator management (201 created, 400 missing data, 404 unknown mining unit, 409 duplicated employee code).
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class OperatorsController(
    IOperatorCommandService operatorCommandService,
    IOperatorQueryService operatorQueryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterOperator([FromBody] RegisterOperatorResource resource)
    {
        var command = RegisterOperatorCommandFromResourceAssembler.ToCommandFromResource(resource);
        var @operator = await operatorCommandService.Handle(command);
        var operatorResource = OperatorResourceFromEntityAssembler.ToResourceFromEntity(@operator);
        return CreatedAtAction(nameof(GetOperatorById), new { operatorId = @operator.Id }, operatorResource);
    }

    [HttpGet]
    public async Task<IActionResult> GetOperators([FromQuery] Guid? miningUnitId)
    {
        var operators = await operatorQueryService.Handle(new GetOperatorsQuery(miningUnitId));
        return Ok(operators.Select(OperatorResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{operatorId:guid}")]
    public async Task<IActionResult> GetOperatorById(Guid operatorId)
    {
        var @operator = await operatorQueryService.Handle(new GetOperatorByIdQuery(operatorId));
        if (@operator is null) return NotFound();
        return Ok(OperatorResourceFromEntityAssembler.ToResourceFromEntity(@operator));
    }

    [HttpPut("{operatorId:guid}")]
    public async Task<IActionResult> UpdateOperator(Guid operatorId, [FromBody] UpdateOperatorResource resource)
    {
        var command = UpdateOperatorCommandFromResourceAssembler.ToCommandFromResource(operatorId, resource);
        var @operator = await operatorCommandService.Handle(command);
        if (@operator is null) return NotFound();
        return Ok(OperatorResourceFromEntityAssembler.ToResourceFromEntity(@operator));
    }
}
