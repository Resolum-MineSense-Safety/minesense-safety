namespace EdgeSynchronizationService.Domain.Model.Commands;

public record SynchronizePendingEventsCommand(string? VehicleId, int BatchSize);
