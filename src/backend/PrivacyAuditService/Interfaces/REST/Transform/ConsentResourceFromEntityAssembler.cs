using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;

namespace PrivacyAuditService.Interfaces.REST.Transform;

public static class ConsentResourceFromEntityAssembler
{
    public static ConsentResource ToResourceFromEntity(ProcessingConsent consent) =>
        new(consent.Id, consent.OperatorId, consent.PolicyVersion, consent.Purposes,
            consent.Status.ToString(), ConsentPolicy.BlocksMonitoring(consent), consent.DecidedAt);
}
