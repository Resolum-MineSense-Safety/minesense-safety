using FleetMonitoringService.Domain.Model.Queries;
using FleetMonitoringService.Domain.Model.ValueObjects;
using MineSenseSafety.Shared.Domain.Model;

namespace FleetMonitoringService.Interfaces.REST.Transform;

public static class GetFleetStatusQueryFromParametersAssembler
{
    public static GetFleetStatusQuery ToQueryFromParameters(
        string? shift, string? fleet, string? location, string? riskLevel) =>
        new(ParseOptional<Shift>(shift, "shift"), fleet, location,
            ParseOptional<RiskLevel>(riskLevel, "risk level"));

    private static TEnum? ParseOptional<TEnum>(string? value, string name) where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        if (!Enum.TryParse<TEnum>(value, ignoreCase: true, out var parsed))
            throw new DomainException($"Unknown {name} '{value}'.");
        return parsed;
    }
}
