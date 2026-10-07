namespace MonitoringService.Interfaces.REST.Resources;

public record SignalEventResource(string Signal, string Type, DateTimeOffset OccurredAt);
