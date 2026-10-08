using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Model.Aggregates;

// Sprint 1 · T13 (Ian Santisteban): mining unit aggregate with its locations (US17). Remaining: deactivation and persistence adapter.
/// <summary>
/// Mining unit of the company (EP08). Once registered it is available to associate
/// fleets, vehicles and people. The business code is unique across the company.
/// </summary>
public class MiningUnit : AggregateRoot
{
    private readonly List<Location> _locations = [];

    public string BusinessCode { get; private set; }
    public string Name { get; private set; }
    public string Region { get; private set; }
    public IReadOnlyList<Location> Locations => _locations.AsReadOnly();

    public MiningUnit(RegisterMiningUnitCommand command)
    {
        MandatoryData.EnsurePresent(
            (nameof(BusinessCode), string.IsNullOrWhiteSpace(command.BusinessCode)),
            (nameof(Name), string.IsNullOrWhiteSpace(command.Name)),
            (nameof(Region), string.IsNullOrWhiteSpace(command.Region)));

        BusinessCode = NormalizeCode(command.BusinessCode);
        Name = command.Name.Trim();
        Region = command.Region.Trim();
    }

    public static string NormalizeCode(string businessCode) => businessCode.Trim().ToUpperInvariant();

    public void Update(UpdateMiningUnitCommand command)
    {
        MandatoryData.EnsurePresent(
            (nameof(Name), string.IsNullOrWhiteSpace(command.Name)),
            (nameof(Region), string.IsNullOrWhiteSpace(command.Region)));

        Name = command.Name.Trim();
        Region = command.Region.Trim();
    }

    public Location AddLocation(string name, LocationKind kind)
    {
        var location = new Location(name, kind);
        if (_locations.Any(existing => string.Equals(existing.Name, location.Name, StringComparison.OrdinalIgnoreCase)))
            throw new DomainException($"Location '{location.Name}' already exists in mining unit {BusinessCode}.");

        _locations.Add(location);
        return location;
    }
}
