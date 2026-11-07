namespace AnalyticsService.Interfaces.REST.Resources;

public record PreventiveMeasureResource(
    Guid Id,
    string Name,
    string MeasureType,
    string Description,
    DateTimeOffset AppliedAt,
    Guid RegisteredBy,
    string? Shift,
    string? MineSite,
    string? Location,
    string? Fleet);
