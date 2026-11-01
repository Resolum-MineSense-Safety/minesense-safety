using MineSenseSafety.Shared.Infrastructure.Persistence.PostgreSql;
using Npgsql;
using OperationsService.Domain.Model.Aggregates;
using OperationsService.Domain.Repositories;

namespace OperationsService.Infrastructure.Persistence.PostgreSql;

/// <summary>Vehicles in PostgreSQL (ADR-005), with the normalized vehicle code unique.</summary>
public class PostgresVehicleRepository(NpgsqlDataSource dataSource)
    : PostgresRepository<Vehicle>(dataSource, "vehicles"), IVehicleRepository
{
    protected override IReadOnlyList<PostgresColumn<Vehicle>> Columns { get; } =
    [
        new("code", "text NOT NULL", vehicle => vehicle.Code),
        new("fleet_id", "uuid NOT NULL", vehicle => vehicle.FleetId),
        new("status", "text NOT NULL", vehicle => vehicle.Status.ToString())
    ];

    protected override IEnumerable<string> Indexes() =>
    [
        "CREATE UNIQUE INDEX IF NOT EXISTS ux_vehicles_code ON vehicles (code)",
        "CREATE INDEX IF NOT EXISTS ix_vehicles_fleet_id ON vehicles (fleet_id, code)"
    ];

    public Task<Vehicle?> FindByCodeAsync(string code) =>
        FindOneAsync("WHERE code = $1", Vehicle.NormalizeCode(code));

    public async Task<IEnumerable<Vehicle>> FindByFleetIdAsync(Guid fleetId) =>
        await QueryAsync("WHERE fleet_id = $1 ORDER BY code, id", fleetId);
}
