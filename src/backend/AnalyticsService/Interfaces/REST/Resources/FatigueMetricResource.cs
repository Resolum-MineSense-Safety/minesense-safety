namespace AnalyticsService.Interfaces.REST.Resources;

public record FatigueMetricResource(
    Guid Id,
    Guid OperatorId,
    Guid AlertId,
    string Severity,
    DateTimeOffset OccurredAt,
    string Shift,
    string MineSite,
    string Location,
    string Fleet);
