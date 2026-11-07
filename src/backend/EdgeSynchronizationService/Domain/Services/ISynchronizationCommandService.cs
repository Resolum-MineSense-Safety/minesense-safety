using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.Commands;

namespace EdgeSynchronizationService.Domain.Services;

public interface ISynchronizationCommandService
{
    Task<(SynchronizationBatch Batch, bool IsDuplicate)> Handle(SubmitSynchronizationBatchCommand command);
}
