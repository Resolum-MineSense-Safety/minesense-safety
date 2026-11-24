namespace PrivacyAuditService.Domain.Model.ValueObjects;

/// <summary>Operator decision about a data-processing policy version (US20).</summary>
public enum ConsentStatus
{
    Accepted,
    Declined,
    Revoked
}
