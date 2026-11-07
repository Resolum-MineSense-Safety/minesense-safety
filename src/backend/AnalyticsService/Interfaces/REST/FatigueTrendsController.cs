using System.Net.Mime;
using AnalyticsService.Domain.Model.Queries;
using AnalyticsService.Domain.Model.ValueObjects;
using AnalyticsService.Domain.Services;
using AnalyticsService.Interfaces.REST.Resources;
using AnalyticsService.Interfaces.REST.Transform;
using Microsoft.AspNetCore.Mvc;
using MineSenseSafety.Shared.Domain.Model;

namespace AnalyticsService.Interfaces.REST;

[ApiController]
[Route("api/v1/analytics/[controller]")]
[Produces(MediaTypeNames.Application.Json)]
public class FatigueTrendsController(IAnalyticsQueryService analyticsQueryService, TimeProvider timeProvider)
    : ControllerBase
{
    /// <summary>
    /// TS-22: alert counts and proportions grouped by Shift, MineSite, Location or Fleet.
    /// The period defaults to the last 30 days; small samples carry a representativeness warning.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(FatigueTrendResource), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> GetFatigueTrends(
        [FromQuery] DateTimeOffset? from, [FromQuery] DateTimeOffset? to, [FromQuery] string groupBy = "Shift",
        [FromQuery] string? shift = null, [FromQuery] string? mineSite = null,
        [FromQuery] string? location = null, [FromQuery] string? fleet = null)
    {
        if (!Enum.TryParse<AnalysisDimension>(groupBy, ignoreCase: true, out var dimension))
            throw new DomainException($"Unknown analysis dimension '{groupBy}'.");

        var periodEnd = to ?? timeProvider.GetUtcNow();
        var period = new AnalysisPeriod(from ?? periodEnd.AddDays(-30), periodEnd);
        var query = new GetFatigueTrendsQuery(period, dimension, new ContextFilter(shift, mineSite, location, fleet));

        var trend = await analyticsQueryService.Handle(query);
        return Ok(FatigueTrendResourceFromReadModelAssembler.ToResourceFromReadModel(trend));
    }
}
