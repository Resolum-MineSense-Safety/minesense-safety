using AnalyticsService.Domain.Model.ValueObjects;

namespace AnalyticsService.Domain.Model.Commands;

public record RecordFatigueMetricCommand(
    Guid OperatorId,
    Guid AlertId,
    FatigueSeverity Severity,
    DateTimeOffset OccurredAt,
    string Shift,
    string MineSite,
    string Location,
    string Fleet);
