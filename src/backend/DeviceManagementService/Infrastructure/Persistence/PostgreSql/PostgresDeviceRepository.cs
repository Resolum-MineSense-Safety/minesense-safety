using DeviceManagementService.Domain.Model.Aggregates;
using DeviceManagementService.Domain.Repositories;
using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;

namespace DeviceManagementService.Infrastructure.Persistence.PostgreSql;

/// <summary>
/// Cabin devices in PostgreSQL (ADR-005). The device code is unique, as registration requires;
/// the operator assignment is indexed for the pre-shift availability check (US06).
/// </summary>
public class PostgresDeviceRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<Device>(dataSource, "devices"), IDeviceRepository
{
    protected override IReadOnlyList<PostgresColumn<Device>> Columns { get; } =
    [
        new("device_code", "text NOT NULL", device => device.DeviceCode),
        new("device_type", "text NOT NULL", device => device.DeviceType),
        new("organization_id", "uuid NOT NULL", device => device.OrganizationId),
        new("assigned_operator_id", "uuid", device => device.AssignedOperatorId),
        new("assigned_vehicle_id", "uuid", device => device.AssignedVehicleId),
        new("status", "text", device => device.Status?.ToString()),
        new("battery_percentage", "numeric(5,2)", device => device.BatteryLevel?.Percentage),
        new("reported_at", "timestamptz", device => Utc(device.ReportedAt))
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_devices_device_code ON devices (device_code)",
        "CREATE INDEX IF NOT EXISTS ix_devices_assigned_operator_id ON devices (assigned_operator_id) WHERE assigned_operator_id IS NOT NULL"
    ];

    public Task<Device?> FindByDeviceCodeAsync(string deviceCode) =>
        FindOneAsync("WHERE device_code = $1", deviceCode);

    public async Task<IEnumerable<Device>> FindByAssignedOperatorIdAsync(Guid operatorId) =>
        await QueryAsync("WHERE assigned_operator_id = $1 ORDER BY device_code, id", operatorId);
}
