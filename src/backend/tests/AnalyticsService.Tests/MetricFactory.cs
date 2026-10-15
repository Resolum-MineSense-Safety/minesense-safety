using AnalyticsService.Domain.Model.Aggregates;
using AnalyticsService.Domain.Model.Commands;
using AnalyticsService.Domain.Model.ValueObjects;

namespace AnalyticsService.Tests;

/// <summary>Builds observations with sensible defaults so each test only states what matters.</summary>
internal static class MetricFactory
{
    public static readonly DateTimeOffset Start = new(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);

    public static RecordFatigueMetricCommand Command(DateTimeOffset occurredAt, string shift = "Night",
        string mineSite = "Antamina", string fleet = "CAT 797F", Guid? operatorId = null,
        FatigueSeverity severity = FatigueSeverity.Warning, string location = "Pit 2") =>
        new(operatorId ?? Guid.NewGuid(), Guid.NewGuid(), severity, occurredAt, shift, mineSite, location, fleet);

    public static FatigueMetric Metric(DateTimeOffset occurredAt, string shift = "Night",
        string mineSite = "Antamina", string fleet = "CAT 797F", Guid? operatorId = null,
        FatigueSeverity severity = FatigueSeverity.Warning) =>
        new(Command(occurredAt, shift, mineSite, fleet, operatorId, severity));

    /// <summary>One observation per hour starting at <paramref name="from"/>.</summary>
    public static IEnumerable<FatigueMetric> Hourly(DateTimeOffset from, int count, string shift = "Night",
        Guid? operatorId = null) =>
        Enumerable.Range(0, count).Select(hour => Metric(from.AddHours(hour), shift, operatorId: operatorId));
}
