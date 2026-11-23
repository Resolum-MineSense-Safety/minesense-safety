using FeedbackReliabilityService.Application.Internal.QueryServices;
using FeedbackReliabilityService.Domain.Model.Queries;
using FeedbackReliabilityService.Domain.Model.ValueObjects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FeedbackReliabilityService.Interfaces.REST;

[ApiController]
[Authorize(Roles = "Supervisor,Manager,SafetyManager,Administrator")]
[Route("api/v1/analytics/alert-reliability")]
[Produces("application/json")]
public class AlertReliabilityController(FeedbackQueryService queries) : ControllerBase
{
    /// <summary>US28 / TS-28: counts and proportions of evaluated alerts in [from, to).</summary>
    [HttpGet]
    [ProducesResponseType(typeof(ReliabilityMetric), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Get([FromQuery] Guid operationId, [FromQuery] DateTimeOffset from,
        [FromQuery] DateTimeOffset to, CancellationToken cancellationToken)
    {
        var query = new EvaluationQuery(operationId, from, to);
        if (!FeedbackAlertsController.ValidQuery(query)) return BadRequest(new { message = "Invalid operation or period." });
        return Ok(await queries.ReliabilityAsync(query, cancellationToken));
    }
}
