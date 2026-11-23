using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.PostgreSql;

/// <summary>
/// Operator assignments in PostgreSQL (ADR-005). Superseded and ended assignments stay as the
/// history of the operational context; the overlap rules remain in <c>AssignmentConflictPolicy</c>.
/// </summary>
public class PostgresOperatorAssignmentRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<OperatorAssignment>(dataSource, "operator_assignments"), IOperatorAssignmentRepository
{
    protected override IReadOnlyList<PostgresColumn<OperatorAssignment>> Columns { get; } =
    [
        new("operator_id", "uuid NOT NULL", assignment => assignment.OperatorId),
        new("vehicle_id", "uuid NOT NULL", assignment => assignment.VehicleId),
        new("shift", "text NOT NULL", assignment => assignment.Shift.ToString()),
        new("valid_from", "date NOT NULL", assignment => assignment.ValidFrom),
        new("valid_to", "date", assignment => assignment.ValidTo),
        new("status", "text NOT NULL", assignment => assignment.Status.ToString()),
        new("created_at", "timestamptz NOT NULL", assignment => Utc(assignment.CreatedAt))
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE INDEX IF NOT EXISTS ix_operator_assignments_operator_id ON operator_assignments (operator_id, valid_from DESC)",
        "CREATE INDEX IF NOT EXISTS ix_operator_assignments_vehicle_id ON operator_assignments (vehicle_id, valid_from DESC)"
    ];

    public async Task<IEnumerable<OperatorAssignment>> FindByOperatorIdAsync(Guid operatorId) =>
        await QueryAsync("WHERE operator_id = $1 ORDER BY valid_from DESC, created_at DESC, id", operatorId);

    public async Task<IEnumerable<OperatorAssignment>> FindByVehicleIdAsync(Guid vehicleId) =>
        await QueryAsync("WHERE vehicle_id = $1 ORDER BY valid_from DESC, created_at DESC, id", vehicleId);
}
