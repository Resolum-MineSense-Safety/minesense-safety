namespace PrivacyAuditService.Domain.Model.ValueObjects;

/// <summary>Outcome of a sensitive-data access attempt (US21, ADR-007).</summary>
public enum AccessDecision
{
    Granted,
    Denied
}
