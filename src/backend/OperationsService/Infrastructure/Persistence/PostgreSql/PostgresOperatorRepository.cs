using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.PostgreSql;

/// <summary>Operators in PostgreSQL (ADR-005), with the normalized employee code unique.</summary>
public class PostgresOperatorRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<Operator>(dataSource, "operators"), IOperatorRepository
{
    protected override IReadOnlyList<PostgresColumn<Operator>> Columns { get; } =
    [
        new("employee_code", "text NOT NULL", @operator => @operator.EmployeeCode),
        new("mining_unit_id", "uuid NOT NULL", @operator => @operator.MiningUnitId),
        new("status", "text NOT NULL", @operator => @operator.Status.ToString())
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_operators_employee_code ON operators (employee_code)",
        "CREATE INDEX IF NOT EXISTS ix_operators_mining_unit_id ON operators (mining_unit_id, employee_code)"
    ];

    public Task<Operator?> FindByEmployeeCodeAsync(string employeeCode) =>
        FindOneAsync("WHERE employee_code = $1", Operator.NormalizeCode(employeeCode));

    public async Task<IEnumerable<Operator>> FindByMiningUnitIdAsync(Guid miningUnitId) =>
        await QueryAsync("WHERE mining_unit_id = $1 ORDER BY employee_code, id", miningUnitId);
}
