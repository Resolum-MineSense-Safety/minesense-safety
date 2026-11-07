using FleetMonitoringService.Domain.Model.ValueObjects;

namespace FleetMonitoringService.Domain.Model.Commands;

public record UpdateOperatorRiskLevelCommand(Guid OperatorId, RiskLevel RiskLevel);
