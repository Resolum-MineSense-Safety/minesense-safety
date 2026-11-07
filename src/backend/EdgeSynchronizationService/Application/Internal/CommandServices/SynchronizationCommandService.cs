using EdgeSynchronizationService.Domain.Model.Aggregates;
using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Domain.Repositories;
using EdgeSynchronizationService.Domain.Services;

namespace EdgeSynchronizationService.Application.Internal.CommandServices;

public sealed class SynchronizationCommandService(
    ISynchronizationBatchRepository repository,
    TimeProvider timeProvider) : ISynchronizationCommandService
{
    public async Task<(SynchronizationBatch Batch, bool IsDuplicate)> Handle(
        SubmitSynchronizationBatchCommand command)
    {
        var existing = await repository.FindByBatchIdAsync(command.BatchId);
        if (existing is not null)
            return (existing, true);

        var batch = new SynchronizationBatch(command, timeProvider.GetUtcNow());
        if (!await repository.TryAddAsync(batch))
            return ((await repository.FindByBatchIdAsync(command.BatchId))!, true);

        return (batch, false);
    }
}
