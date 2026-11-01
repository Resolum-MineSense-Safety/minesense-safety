using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.PostgreSql;

/// <summary>
/// Fleets in PostgreSQL (ADR-005). A fleet name is unique inside its mining unit ignoring case,
/// enforced by a unique index on the mining unit and the upper-case name.
/// </summary>
public class PostgresFleetRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<Fleet>(dataSource, "fleets"), IFleetRepository
{
    protected override IReadOnlyList<PostgresColumn<Fleet>> Columns { get; } =
    [
        new("mining_unit_id", "uuid NOT NULL", fleet => fleet.MiningUnitId),
        new("name", "text NOT NULL", fleet => fleet.Name),
        new("name_key", "text NOT NULL", fleet => fleet.Name.ToUpperInvariant())
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_fleets_mining_unit_name ON fleets (mining_unit_id, name_key)"
    ];

    public async Task<IEnumerable<Fleet>> FindByMiningUnitIdAsync(Guid miningUnitId) =>
        await QueryAsync("WHERE mining_unit_id = $1 ORDER BY name, id", miningUnitId);
}
