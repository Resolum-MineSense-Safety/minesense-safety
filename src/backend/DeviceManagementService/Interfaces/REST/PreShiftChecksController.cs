using System.Net.Mime;
using DeviceManagementService.Domain.Model.Commands;
using DeviceManagementService.Domain.Model.Queries;
using DeviceManagementService.Domain.Services;
using DeviceManagementService.Interfaces.REST.Resources;
using DeviceManagementService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;

namespace DeviceManagementService.Interfaces.REST;

// Sprint 1 · T03 (Carlos Onofre): exposes the pre-shift verification result.
// Remaining: dashboard view in src/frontend.

[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class PreShiftChecksController(
    IPreShiftCheckCommandService preShiftCheckCommandService,
    IPreShiftCheckQueryService preShiftCheckQueryService) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> RunPreShiftCheck([FromBody] RunPreShiftCheckResource resource)
    {
        var check = await preShiftCheckCommandService.Handle(
            new RunPreShiftCheckCommand(resource.OperatorId, resource.VehicleCode));
        var checkResource = PreShiftCheckResourceFromEntityAssembler.ToResourceFromEntity(check);
        return CreatedAtAction(nameof(GetPreShiftCheckById), new { preShiftCheckId = check.Id }, checkResource);
    }

    [HttpGet("{preShiftCheckId:guid}")]
    public async Task<IActionResult> GetPreShiftCheckById(Guid preShiftCheckId)
    {
        var check = await preShiftCheckQueryService.Handle(new GetPreShiftCheckByIdQuery(preShiftCheckId));
        if (check is null) return NotFound();
        return Ok(PreShiftCheckResourceFromEntityAssembler.ToResourceFromEntity(check));
    }

    [HttpGet("latest")]
    public async Task<IActionResult> GetLatestPreShiftCheck([FromQuery] Guid operatorId)
    {
        var check = await preShiftCheckQueryService.Handle(new GetLatestPreShiftCheckByOperatorIdQuery(operatorId));
        if (check is null) return NotFound();
        return Ok(PreShiftCheckResourceFromEntityAssembler.ToResourceFromEntity(check));
    }
}
