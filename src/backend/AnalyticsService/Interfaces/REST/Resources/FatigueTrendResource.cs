namespace AnalyticsService.Interfaces.REST.Resources;

public record TrendGroupResource(string Key, int Alerts, int CriticalAlerts, double Proportion, bool IsRepresentative);

public record FatigueTrendResource(
    DateTimeOffset From,
    DateTimeOffset To,
    string GroupBy,
    int TotalAlerts,
    bool IsRepresentative,
    string? Warning,
    IEnumerable<TrendGroupResource> Groups);
