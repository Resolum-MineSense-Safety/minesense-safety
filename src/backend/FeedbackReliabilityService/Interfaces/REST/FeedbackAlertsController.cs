using FeedbackReliabilityService.Application.Internal.QueryServices;
using FeedbackReliabilityService.Domain.Model.Commands;
using FeedbackReliabilityService.Domain.Model.Queries;
using FeedbackReliabilityService.Domain.Services;
using FeedbackReliabilityService.Interfaces.REST.Resources;
using FeedbackReliabilityService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackReliabilityService.Interfaces.REST;

[ApiController]
[Authorize]
[Route("api/v1/feedback-alerts")]
[Produces("application/json")]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
[ProducesResponseType(StatusCodes.Status403Forbidden)]
public class FeedbackAlertsController(IFeedbackCommandService commands, FeedbackQueryService queries) : ControllerBase
{
    /// <summary>Trusted integration registers operational scope; identity/time are verified against Alerts.</summary>
    [HttpPut("{alertId:guid}")]
    [Authorize(Roles = "Service")]
    [ProducesResponseType(typeof(EvaluationResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Track(Guid alertId, TrackAlertResource resource, CancellationToken cancellationToken) =>
        Ok(FeedbackAssembler.ToResource(await commands.Handle(new TrackAlertCommand(alertId, resource.OperationId), cancellationToken)));

    /// <summary>US27: paged queue, including alerts that remain pending evaluation.</summary>
    [HttpGet]
    [Authorize(Roles = "Supervisor,Manager")]
    [ProducesResponseType(typeof(EvaluationPageResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> List([FromQuery] Guid operationId, [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, [FromQuery] string? status, CancellationToken cancellationToken,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var query = new EvaluationQuery(operationId, from, to);
        if (!ValidQuery(query) || page < 1 || page > 1000000 || pageSize is < 1 or > 100 ||
            (status is not null && status is not ("PENDING" or "AWAITING_REVIEW" or "REVIEWED")))
            return BadRequest(new { message = "Invalid operation, period, status or pagination." });
        var result = await queries.ListAsync(query, status, page, pageSize, cancellationToken);
        return Ok(new EvaluationPageResource(result.Items.Select(FeedbackAssembler.ToResource).ToArray(), result.Total, result.Page, result.PageSize));
    }

    public static bool ValidQuery(EvaluationQuery query) => query.OperationId != Guid.Empty &&
        query.From < query.To && query.To - query.From <= TimeSpan.FromDays(366);
}
