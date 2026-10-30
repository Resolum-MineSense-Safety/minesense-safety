using MineSenseSafety.Shared.Domain.Model;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ValueObjects;

namespace PrivacyAuditService.Domain.Model.Aggregates;

/// <summary>
/// Operator decision about a data-processing policy version (US20).
/// Scaffold: shape only, business rules arrive with Fase 1.
/// </summary>
public class ProcessingConsent : AggregateRoot
{
    public Guid OperatorId { get; private set; }
    public string PolicyVersion { get; private set; } = string.Empty;
    public List<string> Purposes { get; private set; } = new();
    public ConsentStatus Status { get; private set; }
    public DateTimeOffset DecidedAt { get; private set; }

    public ProcessingConsent(RegisterConsentDecisionCommand command, DateTimeOffset decidedAt)
    {
        if (command.OperatorId == Guid.Empty)
            throw new DomainException("A consent decision must belong to an operator.");

        OperatorId = command.OperatorId;
        PolicyVersion = command.PolicyVersion;
        Purposes = command.Purposes.ToList();
        Status = command.Status;
        DecidedAt = decidedAt;
    }
}
