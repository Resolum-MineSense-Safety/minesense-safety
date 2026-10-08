using FatigueDetectionService.Domain.Model.Commands;
using FatigueDetectionService.Domain.Model.ValueObjects;
using FatigueDetectionService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace FatigueDetectionService.Domain.Model.Aggregates;

/// <summary>
/// Point-in-time fatigue evaluation of an operator during a monitoring session (EP02).
/// The risk level is derived from the biometric signals by <see cref="FatigueRiskPolicy"/>.
/// </summary>
public class FatigueAssessment : AggregateRoot
{
    public Guid OperatorId { get; private set; }
    public Guid MonitoringSessionId { get; private set; }
    public BiometricSignals Signals { get; private set; }
    public RiskLevel RiskLevel { get; private set; }
    public DateTimeOffset AssessedAt { get; private set; }

    public FatigueAssessment(AssessFatigueCommand command, DateTimeOffset assessedAt)
    {
        if (command.OperatorId == Guid.Empty)
            throw new DomainException("A fatigue assessment must belong to an operator.");

        OperatorId = command.OperatorId;
        MonitoringSessionId = command.MonitoringSessionId;
        Signals = new BiometricSignals(command.Perclos, command.BlinkRatePerMinute, command.HeartRateVariabilityMs);
        RiskLevel = FatigueRiskPolicy.Classify(Signals);
        AssessedAt = assessedAt;
    }
}
