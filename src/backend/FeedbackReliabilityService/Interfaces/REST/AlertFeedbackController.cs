using FeedbackReliabilityService.Application.Internal.QueryServices;
using FeedbackReliabilityService.Domain.Model.Commands;
using FeedbackReliabilityService.Domain.Services;
using FeedbackReliabilityService.Interfaces.REST.Resources;
using FeedbackReliabilityService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackReliabilityService.Interfaces.REST;

[ApiController]
[Authorize]
[Route("api/v1/alerts/{alertId:guid}/feedback")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
public class AlertFeedbackController(IFeedbackCommandService commands, FeedbackQueryService queries) : ControllerBase
{
    /// <summary>US26 / TS-20: register one evaluation per alert and reporter.</summary>
    [HttpPost]
    [Authorize(Roles = "Operator,Supervisor")]
    [ProducesResponseType(typeof(EvaluationResource), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Report(Guid alertId, ReportFeedbackResource resource, CancellationToken cancellationToken)
    {
        var result = await commands.Handle(FeedbackAssembler.ToCommand(alertId, resource), cancellationToken);
        return CreatedAtAction(nameof(Get), new { alertId }, FeedbackAssembler.ToResource(result));
    }

    [HttpGet]
    [ProducesResponseType(typeof(EvaluationResource), StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(Guid alertId, CancellationToken cancellationToken) =>
        Ok(FeedbackAssembler.ToResource(await queries.GetAsync(alertId, cancellationToken)));

    /// <summary>US27: append an auditable supervisor decision without rewriting operator feedback.</summary>
    [HttpPost("reviews")]
    [Authorize(Roles = "Supervisor")]
    [ProducesResponseType(typeof(EvaluationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Review(Guid alertId, ReviewFeedbackResource resource, CancellationToken cancellationToken) =>
        Ok(FeedbackAssembler.ToResource(await commands.Handle(FeedbackAssembler.ToCommand(alertId, resource), cancellationToken)));
}
