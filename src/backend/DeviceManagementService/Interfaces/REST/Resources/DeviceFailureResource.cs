namespace DeviceManagementService.Interfaces.REST.Resources;

public record DeviceFailureResource(string Kind, string? Detail, DateTimeOffset OccurredAt, DateTimeOffset? RecoveredAt);
