using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using MonitoringService.Domain.Model.Aggregates;
using MonitoringService.Domain.Model.ValueObjects;
using MonitoringService.Domain.Repositories;
using Npgsql;

namespace MonitoringService.Infrastructure.Persistence.PostgreSql;

/// <summary>
/// Monitoring sessions in PostgreSQL (ADR-005). A partial unique index on the operator of the open
/// sessions keeps the rule "an operator has at most one open monitoring session" even when two
/// start requests race. The status column is the one the domain policy evaluates on every write.
/// </summary>
public class PostgresMonitoringSessionRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<MonitoringSession>(dataSource, "monitoring_sessions"), IMonitoringSessionRepository
{
    protected override IReadOnlyList<PostgresColumn<MonitoringSession>> Columns { get; } =
    [
        new("operator_id", "uuid NOT NULL", session => session.OperatorId),
        new("vehicle_code", "text NOT NULL", session => session.VehicleCode),
        new("status", "text NOT NULL", session => session.Status.ToString()),
        new("started_at", "timestamptz NOT NULL", session => Utc(session.StartedAt)),
        new("stopped_at", "timestamptz", session => Utc(session.StoppedAt))
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_monitoring_sessions_open_operator ON monitoring_sessions (operator_id) WHERE stopped_at IS NULL",
        "CREATE INDEX IF NOT EXISTS ix_monitoring_sessions_operator_id ON monitoring_sessions (operator_id, started_at DESC)",
        "CREATE INDEX IF NOT EXISTS ix_monitoring_sessions_status ON monitoring_sessions (status, started_at DESC)"
    ];

    public Task<MonitoringSession?> FindOpenByOperatorIdAsync(Guid operatorId) =>
        FindOneAsync("WHERE operator_id = $1 AND stopped_at IS NULL ORDER BY started_at DESC, id", operatorId);

    public Task<MonitoringSession?> FindLatestByOperatorIdAsync(Guid operatorId) =>
        FindOneAsync("WHERE operator_id = $1 ORDER BY started_at DESC, id", operatorId);

    public async Task<IEnumerable<MonitoringSession>> FindByStatusAsync(MonitoringStatus? status) =>
        status is null
            ? await QueryAsync("ORDER BY started_at DESC, id")
            : await QueryAsync("WHERE status = $1 ORDER BY started_at DESC, id", status.Value.ToString());
}
