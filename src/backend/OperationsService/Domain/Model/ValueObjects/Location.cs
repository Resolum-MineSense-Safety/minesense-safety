using MineSenseSafety.Shared.Domain.Model;

namespace OperationsService.Domain.Model.ValueObjects;

/// <summary>
/// Operational place of a mining unit (pit, dump, crusher...). Value object:
/// two locations with the same name and kind are the same location.
/// </summary>
public record Location
{
    public string Name { get; }
    public LocationKind Kind { get; }

    public Location(string name, LocationKind kind)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Missing mandatory data: Name.");

        Name = name.Trim();
        Kind = kind;
    }
}
