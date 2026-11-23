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
    public AssessmentOrigin Origin { get; private set; }

    /// <summary>When an Edge reading reached the cloud; <see cref="AssessedAt"/> keeps the capture time.</summary>
    public DateTimeOffset? SyncedAt { get; private set; }

    /// <summary>Whether this assessment requires a preventive alert.</summary>
    public bool RequiresAlert => RiskLevel is RiskLevel.Warning or RiskLevel.Critical;

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

    /// <summary>
    /// Historical assessment of a reading the cabin captured and classified earlier. Its id comes from
    /// the Edge, so the same reading always maps to the same assessment.
    /// </summary>
    public FatigueAssessment(RecordEdgeAssessmentCommand command, DateTimeOffset syncedAt)
        : this(new AssessFatigueCommand(command.OperatorId, command.MonitoringSessionId, command.Perclos,
            command.BlinkRatePerMinute, command.HeartRateVariabilityMs), command.CapturedAt)
    {
        if (command.AssessmentId == Guid.Empty)
            throw new DomainException("An Edge assessment must keep the id of its reading.");

        Id = command.AssessmentId;
        Origin = AssessmentOrigin.Edge;
        SyncedAt = syncedAt;
    }
}
