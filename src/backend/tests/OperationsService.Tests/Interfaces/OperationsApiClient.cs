using System.Net.Http.Json;
using OperationsService.Interfaces.REST.Resources;

namespace OperationsService.Tests.Interfaces;

public record ErrorResource(string Message);

/// <summary>Builds the operational structure through the HTTP API so each test starts from fresh, unique data.</summary>
public static class OperationsApiClient
{
    public static string UniqueCode(string prefix) => $"{prefix}-{Guid.NewGuid():N}"[..20].ToUpperInvariant();

    public static async Task<T> ReadAsync<T>(this HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task<MiningUnitResource> RegisterMiningUnitAsync(this HttpClient client) =>
        await (await client.PostAsJsonAsync("/api/v1/mining-units",
            new RegisterMiningUnitResource(UniqueCode("UM"), "Antamina", "Ancash"))).ReadAsync<MiningUnitResource>();

    public static async Task<FleetResource> RegisterFleetAsync(this HttpClient client, Guid? miningUnitId = null)
    {
        miningUnitId ??= (await client.RegisterMiningUnitAsync()).Id;
        return await (await client.PostAsJsonAsync("/api/v1/fleets",
            new RegisterFleetResource(miningUnitId.Value, "Acarreo 1"))).ReadAsync<FleetResource>();
    }

    public static async Task<VehicleResource> RegisterVehicleAsync(this HttpClient client, Guid? fleetId = null)
    {
        fleetId ??= (await client.RegisterFleetAsync()).Id;
        return await (await client.PostAsJsonAsync("/api/v1/vehicles",
            new RegisterVehicleResource(UniqueCode("CAT"), "Caterpillar 797F", fleetId.Value))).ReadAsync<VehicleResource>();
    }

    public static async Task<OperatorAssignmentResource> CreateAssignmentAsync(
        this HttpClient client, Guid operatorId, Guid vehicleId, DateOnly validFrom, string shift = "Day") =>
        await (await client.PostAsJsonAsync("/api/v1/operator-assignments",
                new CreateAssignmentResource(operatorId, vehicleId, shift, validFrom, null)))
            .ReadAsync<OperatorAssignmentResource>();
}
