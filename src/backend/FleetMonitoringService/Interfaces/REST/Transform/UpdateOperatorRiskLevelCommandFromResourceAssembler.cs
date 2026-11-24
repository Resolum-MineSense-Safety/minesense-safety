using FleetMonitoringService.Domain.Model.Commands;
using FleetMonitoringService.Domain.Model.ValueObjects;
using FleetMonitoringService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace FleetMonitoringService.Interfaces.REST.Transform;

public static class UpdateOperatorRiskLevelCommandFromResourceAssembler
{
    public static UpdateOperatorRiskLevelCommand ToCommandFromResource(
        Guid operatorId, UpdateOperatorRiskLevelResource resource)
    {
        if (!Enum.TryParse<RiskLevel>(resource.RiskLevel, ignoreCase: true, out var riskLevel))
            throw new DomainException($"Unknown risk level '{resource.RiskLevel}'.");

        return new UpdateOperatorRiskLevelCommand(operatorId, riskLevel);
    }
}
