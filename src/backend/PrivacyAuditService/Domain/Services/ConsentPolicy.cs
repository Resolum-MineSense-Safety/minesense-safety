using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.ValueObjects;

namespace PrivacyAuditService.Domain.Services;

/// <summary>
/// Enforcement rules for consent decisions (US20, scenario 3).
/// </summary>
public static class ConsentPolicy
{
    public static bool BlocksMonitoring(ProcessingConsent consent)
    {
        if (consent.Status is ConsentStatus.Declined or ConsentStatus.Revoked)
            return true;

        var accepted = consent.Purposes.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return ProcessingPolicyCatalog.MandatoryPurposes().Any(mandatory => !accepted.Contains(mandatory));
    }
}
