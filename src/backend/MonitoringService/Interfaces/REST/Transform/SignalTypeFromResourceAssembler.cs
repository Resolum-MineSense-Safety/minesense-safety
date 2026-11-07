using MonitoringService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace MonitoringService.Interfaces.REST.Transform;

public static class SignalTypeFromResourceAssembler
{
    public static SignalType ToSignalType(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)
            || int.TryParse(value, out _)
            || !Enum.TryParse<SignalType>(value, ignoreCase: true, out var signal))
            throw new DomainException($"Unknown signal '{value}'.");

        return signal;
    }

    public static MonitoringStatus? ToMonitoringStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (int.TryParse(value, out _) || !Enum.TryParse<MonitoringStatus>(value, ignoreCase: true, out var status))
            throw new DomainException($"Unknown monitoring status '{value}'.");

        return status;
    }
}
