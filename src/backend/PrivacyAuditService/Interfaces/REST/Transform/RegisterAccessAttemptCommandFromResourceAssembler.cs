using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Model.ValueObjects;
using PrivacyAuditService.Interfaces.REST.Resources;
using MineSenseSafety.Shared.Domain.Model;

namespace PrivacyAuditService.Interfaces.REST.Transform;

public static class RegisterAccessAttemptCommandFromResourceAssembler
{
    public static RegisterAccessAttemptCommand ToCommandFromResource(RegisterAccessAttemptResource resource)
    {
        if (!Enum.TryParse<AccessDecision>(resource.Decision, ignoreCase: true, out var decision))
            throw new DomainException($"Unknown access decision '{resource.Decision}'.");

        return new RegisterAccessAttemptCommand(resource.UserId, resource.ResourceType,
            resource.ResourceId, resource.Fields, resource.Purpose, decision);
    }
}
