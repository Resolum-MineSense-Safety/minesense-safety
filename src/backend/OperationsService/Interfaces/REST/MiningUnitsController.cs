using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Services;
using OperationsService.Interfaces.REST.Resources;
using OperationsService.Interfaces.REST.Transform;

namespace OperationsService.Interfaces.REST;

// Sprint 1 · T14 (Joseph Huamani): CRUD endpoints for mining units and locations (US17). Remaining: delete/deactivate and admin views in src/frontend.
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class MiningUnitsController(
    IMiningUnitCommandService miningUnitCommandService,
    IMiningUnitQueryService miningUnitQueryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RegisterMiningUnit([FromBody] RegisterMiningUnitResource resource)
    {
        var command = RegisterMiningUnitCommandFromResourceAssembler.ToCommandFromResource(resource);
        var miningUnit = await miningUnitCommandService.Handle(command);
        var miningUnitResource = MiningUnitResourceFromEntityAssembler.ToResourceFromEntity(miningUnit);
        return CreatedAtAction(nameof(GetMiningUnitById), new { miningUnitId = miningUnit.Id }, miningUnitResource);
    }

    [HttpGet]
    public async Task<IActionResult> GetMiningUnits()
    {
        var miningUnits = await miningUnitQueryService.Handle(new GetMiningUnitsQuery());
        return Ok(miningUnits.Select(MiningUnitResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpGet("{miningUnitId:guid}")]
    public async Task<IActionResult> GetMiningUnitById(Guid miningUnitId)
    {
        var miningUnit = await miningUnitQueryService.Handle(new GetMiningUnitByIdQuery(miningUnitId));
        if (miningUnit is null) return NotFound();
        return Ok(MiningUnitResourceFromEntityAssembler.ToResourceFromEntity(miningUnit));
    }

    [HttpPut("{miningUnitId:guid}")]
    public async Task<IActionResult> UpdateMiningUnit(Guid miningUnitId, [FromBody] UpdateMiningUnitResource resource)
    {
        var command = UpdateMiningUnitCommandFromResourceAssembler.ToCommandFromResource(miningUnitId, resource);
        var miningUnit = await miningUnitCommandService.Handle(command);
        if (miningUnit is null) return NotFound();
        return Ok(MiningUnitResourceFromEntityAssembler.ToResourceFromEntity(miningUnit));
    }

    [HttpPost("{miningUnitId:guid}/locations")]
    public async Task<IActionResult> AddLocation(Guid miningUnitId, [FromBody] AddLocationResource resource)
    {
        var command = AddLocationCommandFromResourceAssembler.ToCommandFromResource(miningUnitId, resource);
        var miningUnit = await miningUnitCommandService.Handle(command);
        if (miningUnit is null) return NotFound();
        var miningUnitResource = MiningUnitResourceFromEntityAssembler.ToResourceFromEntity(miningUnit);
        return CreatedAtAction(nameof(GetMiningUnitById), new { miningUnitId = miningUnit.Id }, miningUnitResource);
    }
}
