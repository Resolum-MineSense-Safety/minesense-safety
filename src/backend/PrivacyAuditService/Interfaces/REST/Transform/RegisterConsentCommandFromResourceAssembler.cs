using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace PrivacyAuditService.Interfaces.REST.Transform;

public static class RegisterConsentCommandFromResourceAssembler
{
    public static RegisterConsentDecisionCommand ToCommandFromResource(RegisterConsentResource resource)
    {
        if (!Enum.TryParse<ConsentStatus>(resource.Status, ignoreCase: true, out var status))
            throw new DomainException($"Unknown consent status '{resource.Status}'.");

        if (!ProcessingPolicyCatalog.IsKnownVersion(resource.PolicyVersion))
            throw new DomainException($"Unknown policy version '{resource.PolicyVersion}'.");

        foreach (var purpose in resource.Purposes)
            if (!ProcessingPolicyCatalog.IsKnownPurpose(purpose))
                throw new DomainException($"Unknown processing purpose '{purpose}'.");

        return new RegisterConsentDecisionCommand(resource.OperatorId, resource.PolicyVersion, resource.Purposes, status);
    }
}
