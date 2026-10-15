using PrivacyAuditService.Domain.Model.Aggregates;
using PrivacyAuditService.Domain.Model.Commands;
using PrivacyAuditService.Domain.Repositories;
using PrivacyAuditService.Domain.Services;
using MineSenseSafety.Shared.Domain.Model;

namespace PrivacyAuditService.Application.Internal.CommandServices;

public class PrivacyAuditCommandService(
    IProcessingConsentRepository consentRepository,
    IAccessAuditRepository accessAuditRepository,
    IComplianceReportRepository reportRepository,
    TimeProvider timeProvider) : IPrivacyAuditCommandService
{
    public async Task<(ProcessingConsent Consent, bool Created)> Handle(RegisterConsentDecisionCommand command)
    {
        _ = accessAuditRepository;
        _ = reportRepository;
        if (!ProcessingPolicyCatalog.IsKnownVersion(command.PolicyVersion))
            throw new DomainException($"Unknown policy version '{command.PolicyVersion}'.");

        foreach (var purpose in command.Purposes)
            if (!ProcessingPolicyCatalog.IsKnownPurpose(purpose))
                throw new DomainException($"Unknown processing purpose '{purpose}'.");

        var existing = (await consentRepository.FindByOperatorIdAsync(command.OperatorId))
            .FirstOrDefault(c => string.Equals(c.PolicyVersion, command.PolicyVersion, StringComparison.OrdinalIgnoreCase)
                && c.Status == command.Status
                && c.Purposes.SequenceEqual(command.Purposes, StringComparer.OrdinalIgnoreCase));

        if (existing is not null)
            return (existing, false);

        var consent = new ProcessingConsent(command, timeProvider.GetUtcNow());
        await consentRepository.AddAsync(consent);
        return (consent, true);
    }

    public async Task<AccessAuditEntry> Handle(RegisterAccessAttemptCommand command)
    {
        var entries = await accessAuditRepository.ListAsync();
        var prevHash = entries.OrderByDescending(e => e.AccessedAt).ThenByDescending(e => e.Id)
            .Select(e => e.Hash)
            .FirstOrDefault() ?? AccessAuditHashChain.GenesisPrevHash;

        var entry = new AccessAuditEntry(command, timeProvider.GetUtcNow(), prevHash);
        await accessAuditRepository.AddAsync(entry);
        return entry;
    }
    public Task<ComplianceReport> Handle(RequestSafetyReportCommand command) =>
        throw new NotImplementedException("Fase 2 (US22): report request handling is not implemented yet.");
}
