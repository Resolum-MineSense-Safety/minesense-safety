using AnalyticsService.Domain.Model.ValueObjects;

namespace AnalyticsService.Domain.Model.Queries;

public record GetFatigueTrendsQuery(AnalysisPeriod Period, AnalysisDimension GroupBy, ContextFilter Filter);
