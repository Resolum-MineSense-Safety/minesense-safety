using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.Queries;
using OperationsService.Domain.Services;
using OperationsService.Interfaces.REST.Resources;
using OperationsService.Interfaces.REST.Transform;

namespace OperationsService.Interfaces.REST;

// Sprint 1 · T17 (Jhosep Argomedo): assignment management endpoints (US19). Remaining: admin view in src/frontend.
// Sprint 1 · T18 (Andreow Santiago): GET context validates a monitoring session context for the Monitoring service.
[ApiController]
[Route("api/v1/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class OperatorAssignmentsController(
    IOperatorAssignmentCommandService assignmentCommandService,
    IOperatorAssignmentQueryService assignmentQueryService,
    TimeProvider timeProvider) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> CreateAssignment([FromBody] CreateAssignmentResource resource)
    {
        var command = CreateAssignmentCommandFromResourceAssembler.ToCommandFromResource(resource);
        var assignment = await assignmentCommandService.Handle(command);
        var assignmentResource = OperatorAssignmentResourceFromEntityAssembler.ToResourceFromEntity(assignment);
        return CreatedAtAction(nameof(GetAssignmentById), new { assignmentId = assignment.Id }, assignmentResource);
    }

    [HttpGet("{assignmentId:guid}")]
    public async Task<IActionResult> GetAssignmentById(Guid assignmentId)
    {
        var assignment = await assignmentQueryService.Handle(new GetAssignmentByIdQuery(assignmentId));
        if (assignment is null) return NotFound();
        return Ok(OperatorAssignmentResourceFromEntityAssembler.ToResourceFromEntity(assignment));
    }

    /// <summary>Active assignment of the operator on <paramref name="date"/> (today when omitted).</summary>
    [HttpGet("active")]
    public async Task<IActionResult> GetActiveAssignmentByOperator([FromQuery] Guid operatorId, [FromQuery] DateOnly? date)
    {
        var query = new GetActiveAssignmentByOperatorQuery(operatorId, date ?? Today());
        var assignment = await assignmentQueryService.Handle(query);
        if (assignment is null) return NotFound();
        return Ok(OperatorAssignmentResourceFromEntityAssembler.ToResourceFromEntity(assignment));
    }

    /// <summary>Full assignment history of the operator, including superseded and ended assignments.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAssignmentHistoryByOperator([FromQuery] Guid operatorId)
    {
        var assignments = await assignmentQueryService.Handle(new GetAssignmentHistoryByOperatorQuery(operatorId));
        return Ok(assignments.Select(OperatorAssignmentResourceFromEntityAssembler.ToResourceFromEntity));
    }

    [HttpPost("{assignmentId:guid}/vehicle-change")]
    public async Task<IActionResult> ChangeAssignedVehicle(Guid assignmentId, [FromBody] ChangeAssignedVehicleResource resource)
    {
        var command = new ChangeAssignedVehicleCommand(assignmentId, resource.NewVehicleId, resource.ChangeDate);
        var replacement = await assignmentCommandService.Handle(command);
        if (replacement is null) return NotFound();
        var assignmentResource = OperatorAssignmentResourceFromEntityAssembler.ToResourceFromEntity(replacement);
        return CreatedAtAction(nameof(GetAssignmentById), new { assignmentId = replacement.Id }, assignmentResource);
    }

    [HttpPost("{assignmentId:guid}/end")]
    public async Task<IActionResult> EndAssignment(Guid assignmentId, [FromBody] EndAssignmentResource resource)
    {
        var assignment = await assignmentCommandService.Handle(new EndAssignmentCommand(assignmentId, resource.EndDate));
        if (assignment is null) return NotFound();
        return Ok(OperatorAssignmentResourceFromEntityAssembler.ToResourceFromEntity(assignment));
    }

    /// <summary>
    /// T18: tells the Monitoring service whether <paramref name="operatorId"/> may run a session on
    /// <paramref name="vehicleCode"/> on <paramref name="date"/> (today when omitted). Always 200 with valid/reason.
    /// </summary>
    [HttpGet("context")]
    public async Task<IActionResult> ValidateOperationalContext(
        [FromQuery] Guid operatorId, [FromQuery] string? vehicleCode, [FromQuery] DateOnly? date)
    {
        var query = new ValidateOperationalContextQuery(operatorId, vehicleCode ?? string.Empty, date ?? Today());
        var validation = await assignmentQueryService.Handle(query);
        return Ok(OperationalContextResourceFromValidationAssembler.ToResourceFromValidation(validation));
    }

    private DateOnly Today() => DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
}
