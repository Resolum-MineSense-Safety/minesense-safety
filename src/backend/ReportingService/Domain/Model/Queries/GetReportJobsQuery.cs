namespace ReportingService.Domain.Model.Queries;

public record GetReportJobsQuery(
    string? SiteId,
    string? FleetId,
    DateTimeOffset? From,
    DateTimeOffset? To,
    int Page,
    int PageSize);
