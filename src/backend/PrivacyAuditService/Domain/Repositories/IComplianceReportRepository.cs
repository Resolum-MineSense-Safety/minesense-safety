using MineSenseSafety.Shared.Domain.Repositories;
using PrivacyAuditService.Domain.Model.Aggregates;

namespace PrivacyAuditService.Domain.Repositories;

public interface IComplianceReportRepository : IBaseRepository<ComplianceReport>
{
}
