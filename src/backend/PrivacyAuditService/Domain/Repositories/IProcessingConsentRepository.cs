using MineSenseSafety.Shared.Domain.Repositories;
using PrivacyAuditService.Domain.Model.Aggregates;

namespace PrivacyAuditService.Domain.Repositories;

public interface IProcessingConsentRepository : IBaseRepository<ProcessingConsent>
{
    Task<IEnumerable<ProcessingConsent>> FindByOperatorIdAsync(Guid operatorId);
}
