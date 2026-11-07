using PrivacyAuditService.Domain.Services;
using PrivacyAuditService.Interfaces.REST.Resources;

namespace PrivacyAuditService.Interfaces.REST.Transform;

public static class ProcessingPurposesResourceFromEntityAssembler
{
    public static ProcessingPurposesResource ToResource() =>
        new(ProcessingPolicyCatalog.CurrentVersion,
            ProcessingPolicyCatalog.Purposes.Select(p => new ProcessingPurposeResource(p.Code, p.Description, p.IsMandatory)));
}
