using AlertService.Domain.Model.ValueObjects;

namespace AlertService.Domain.Model.Queries;

public record GetAlertsByStatusQuery(AlertStatus Status);
