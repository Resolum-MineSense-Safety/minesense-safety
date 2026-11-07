namespace AnalyticsService.Domain.Model.Queries;

/// <summary>Compares the same window length before and after the measure.</summary>
public record EvaluatePreventiveMeasureQuery(Guid MeasureId, int WindowDays);
