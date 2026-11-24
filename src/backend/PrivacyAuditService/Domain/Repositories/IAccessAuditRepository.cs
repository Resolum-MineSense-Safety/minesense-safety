using MineSenseSafety.Shared.Domain.Repositories;
using PrivacyAuditService.Domain.Model.Aggregates;

namespace PrivacyAuditService.Domain.Repositories;

public interface IAccessAuditRepository : IBaseRepository<AccessAuditEntry>
{
    Task<IEnumerable<AccessAuditEntry>> FindByResourceAsync(string resourceType, Guid resourceId);
}
