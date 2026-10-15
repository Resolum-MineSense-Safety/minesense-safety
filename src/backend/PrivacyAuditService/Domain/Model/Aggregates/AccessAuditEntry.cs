using MineSenseSafety.Shared.Domain.Model;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Domain.Services;

namespace PrivacyAuditService.Domain.Model.Aggregates;

/// <summary>
/// Immutable record of a sensitive-data access attempt: who, what, when and declared purpose (US21).
/// Append-only by design: no update or delete operations exist for this aggregate.
/// Scaffold: shape only, hash-chain arrives with Fase 1.
/// </summary>
public class AccessAuditEntry : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string ResourceType { get; private set; } = string.Empty;
    public Guid ResourceId { get; private set; }
    public List<string> Fields { get; private set; } = new();
    public string Purpose { get; private set; } = string.Empty;
    public AccessDecision Decision { get; private set; }
    public DateTimeOffset AccessedAt { get; private set; }
    public string PrevHash { get; private set; } = string.Empty;
    public string Hash { get; private set; } = string.Empty;

    public AccessAuditEntry(RegisterAccessAttemptCommand command, DateTimeOffset accessedAt, string prevHash)
    {
        if (command.UserId == Guid.Empty)
            throw new DomainException("An access audit entry must belong to a user.");
        if (string.IsNullOrWhiteSpace(command.ResourceType))
            throw new DomainException("An access audit entry must identify the resource type.");
        if (command.ResourceId == Guid.Empty)
            throw new DomainException("An access audit entry must identify the resource.");
        if (string.IsNullOrWhiteSpace(command.Purpose))
            throw new DomainException("An access audit entry must declare its purpose.");

        UserId = command.UserId;
        ResourceType = command.ResourceType;
        ResourceId = command.ResourceId;
        Fields = command.Fields.ToList();
        Purpose = command.Purpose;
        Decision = command.Decision;
        AccessedAt = accessedAt;
        PrevHash = prevHash;
        Hash = AccessAuditHashChain.Compute(this);
    }
}
