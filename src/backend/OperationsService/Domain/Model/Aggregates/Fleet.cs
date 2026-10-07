using MineSenseSafety.Shared.Domain.Model;
using OperationsService.Domain.Model.Commands;
using OperationsService.Domain.Model.ValueObjects;

namespace OperationsService.Domain.Model.Aggregates;

// Sprint 1 · T13 (Ian Santisteban): fleet aggregate grouped by mining unit (US17). Its name is unique inside the unit.
public class Fleet : AggregateRoot
{
    public Guid MiningUnitId { get; private set; }
    public string Name { get; private set; }

    public Fleet(RegisterFleetCommand command)
    {
        MandatoryData.EnsurePresent(
            (nameof(MiningUnitId), command.MiningUnitId == Guid.Empty),
            (nameof(Name), string.IsNullOrWhiteSpace(command.Name)));

        MiningUnitId = command.MiningUnitId;
        Name = command.Name.Trim();
    }
}
