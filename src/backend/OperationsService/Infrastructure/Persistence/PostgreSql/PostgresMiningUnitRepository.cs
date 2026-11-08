using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.PostgreSql;

/// <summary>
/// Mining units in PostgreSQL (ADR-005). The business code is stored normalized (upper case) and
/// unique, so the rule "one mining unit per business code" holds even for concurrent registrations.
/// </summary>
public class PostgresMiningUnitRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<MiningUnit>(dataSource, "mining_units"), IMiningUnitRepository
{
    protected override IReadOnlyList<PostgresColumn<MiningUnit>> Columns { get; } =
    [
        new("business_code", "text NOT NULL", unit => unit.BusinessCode),
        new("name", "text NOT NULL", unit => unit.Name)
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_mining_units_business_code ON mining_units (business_code)"
    ];

    public Task<MiningUnit?> FindByBusinessCodeAsync(string businessCode) =>
        FindOneAsync("WHERE business_code = $1", MiningUnit.NormalizeCode(businessCode));
}
