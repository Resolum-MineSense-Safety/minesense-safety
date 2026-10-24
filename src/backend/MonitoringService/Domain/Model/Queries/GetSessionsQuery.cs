using MonitoringService.Domain.Model.ValueObjects;

namespace MonitoringService.Domain.Model.Queries;

public record GetSessionsQuery(MonitoringStatus? Status = null);
