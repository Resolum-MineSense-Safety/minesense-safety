using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Interfaces.REST.Resources;

namespace FatigueDetectionService.Interfaces.REST.Transform;

public static class AssessFatigueCommandFromResourceAssembler
{
    public static AssessFatigueCommand ToCommandFromResource(AssessFatigueResource resource) =>
        new(resource.OperatorId, resource.MonitoringSessionId, resource.Perclos,
            resource.BlinkRatePerMinute, resource.HeartRateVariabilityMs);
}
