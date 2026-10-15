using AnalyticsService.Domain.Model.ValueObjects;

namespace AnalyticsService.Domain.Model.Commands;

public record RegisterPreventiveMeasureCommand(
    string Name,
    PreventiveMeasureType MeasureType,
    string? Description,
    DateTimeOffset AppliedAt,
    ContextFilter Scope,
    Guid RegisteredBy);
