using AnalyticsService.Domain.Model.ValueObjects;

namespace AnalyticsService.Domain.Model.ReadModels;

/// <summary>Alert frequency of one group (a shift, a mine...).</summary>
public record TrendGroup(string Key, int Alerts, int CriticalAlerts, double Proportion, bool IsRepresentative);

/// <summary>
/// Result of TS-22: counts and proportions per group under the same criteria.
/// When the sample is too small, <see cref="Warning"/> explains the limitation.
/// </summary>
public record FatigueTrend(
    AnalysisPeriod Period,
    AnalysisDimension GroupBy,
    int TotalAlerts,
    IReadOnlyList<TrendGroup> Groups,
    bool IsRepresentative,
    string? Warning);
