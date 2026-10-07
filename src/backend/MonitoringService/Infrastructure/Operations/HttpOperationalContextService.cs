using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using MonitoringService.Application.Internal.OutboundServices;

namespace MonitoringService.Infrastructure.Operations;

// Sprint 1 · T18 (Andreow Santiago): HTTP adapter validating the operational context against the Operations service. Remaining: resilience policies (retry / circuit breaker).
/// <summary>
/// Calls <c>GET {Services:OperationsServiceUrl}/api/v1/operator-assignments/context</c>.
/// Any transport or protocol failure is treated as an invalid context so monitoring never
/// starts without a confirmed assignment.
/// </summary>
public class HttpOperationalContextService(HttpClient httpClient) : IOperationalContextService
{
    public async Task<OperationalContext> ValidateAsync(Guid operatorId, string vehicleCode, DateOnly date)
    {
        var uri = "api/v1/operator-assignments/context" +
                  $"?operatorId={operatorId}" +
                  $"&vehicleCode={Uri.EscapeDataString(vehicleCode ?? string.Empty)}" +
                  $"&date={date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}";

        try
        {
            using var response = await httpClient.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                return OperationalContext.Invalid(
                    $"The Operations service could not validate the context (HTTP {(int)response.StatusCode}).");

            var body = await response.Content.ReadFromJsonAsync<OperationalContextResponse>();
            return body is null
                ? OperationalContext.Invalid("The Operations service returned an empty context.")
                : new OperationalContext(body.Valid, body.AssignmentId, body.Reason);
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return OperationalContext.Invalid($"The Operations service is not reachable: {exception.Message}");
        }
    }

    private sealed record OperationalContextResponse(bool Valid, Guid? AssignmentId, string? Reason);
}
