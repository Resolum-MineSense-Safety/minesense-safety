using FatigueDetectionService.Domain.Model.Aggregates;
using MineSenseSafety.Shared.Domain.Repositories;

namespace FatigueDetectionService.Domain.Repositories;

public interface IFatigueAssessmentRepository : IBaseRepository<FatigueAssessment>
{
    /// <summary>Returns the assessments of an operator, newest first.</summary>
    Task<IEnumerable<FatigueAssessment>> FindByOperatorIdAsync(Guid operatorId);
}
