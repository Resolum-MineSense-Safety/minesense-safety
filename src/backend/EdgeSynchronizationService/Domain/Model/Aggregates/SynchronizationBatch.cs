using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace EdgeSynchronizationService.Domain.Model.Aggregates;

public sealed class SynchronizationBatch : AggregateRoot
{
    private readonly List<SynchronizationEvent> _events = [];

    public Guid BatchId { get; private set; }
    public Guid EdgeNodeId { get; private set; }
    public Guid DeviceId { get; private set; }
    public long SequenceNumber { get; private set; }
    public DateTimeOffset CapturedAt { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public SynchronizationStatus Status { get; private set; }
    public IReadOnlyCollection<SynchronizationEvent> Events => _events.AsReadOnly();

    public SynchronizationBatch(SubmitSynchronizationBatchCommand command, DateTimeOffset receivedAt)
    {
        if (command.BatchId == Guid.Empty)
            throw new DomainException("Batch id is required.");
        if (command.EdgeNodeId == Guid.Empty)
            throw new DomainException("Edge node id is required.");
        if (command.DeviceId == Guid.Empty)
            throw new DomainException("Device id is required.");
        if (command.SequenceNumber < 0)
            throw new DomainException("Sequence number cannot be negative.");
        if (command.Events is null || command.Events.Count is < 1 or > 500)
            throw new DomainException("A batch must contain between 1 and 500 events.");
        if (command.Events.Any(item => item.EventId == Guid.Empty))
            throw new DomainException("Event id is required.");
        if (command.Events.Select(item => item.EventId).Distinct().Count() != command.Events.Count)
            throw new DomainException("Event ids must be unique within a batch.");
        if (command.Events.Any(item => string.IsNullOrWhiteSpace(item.EventType)))
            throw new DomainException("Event type is required.");

        BatchId = command.BatchId;
        EdgeNodeId = command.EdgeNodeId;
        DeviceId = command.DeviceId;
        SequenceNumber = command.SequenceNumber;
        CapturedAt = command.CapturedAt;
        ReceivedAt = receivedAt;
        Status = SynchronizationStatus.Accepted;
        _events.AddRange(command.Events);
    }
}
