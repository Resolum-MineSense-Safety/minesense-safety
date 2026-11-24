namespace PrivacyAuditService.Domain.Model.ValueObjects;

/// <summary>Lifecycle of an async compliance report request (US22, ADR-008).</summary>
public enum ReportStatus
{
    Requested,
    Ready,
    Empty
}
