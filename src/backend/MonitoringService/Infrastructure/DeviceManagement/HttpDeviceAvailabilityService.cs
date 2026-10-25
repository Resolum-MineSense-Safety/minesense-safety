using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using MonitoringService.Application.Internal.OutboundServices;

namespace MonitoringService.Infrastructure.DeviceManagement;

/// <summary>
/// Calls <c>GET {Services:DeviceManagementServiceUrl}/api/v1/devices/pre-shift-availability</c>.
/// Any transport or protocol failure is treated as unavailable devices so monitoring never
/// starts without confirmed devices.
/// </summary>
public class HttpDeviceAvailabilityService(HttpClient httpClient, decimal batteryThreshold)
    : IDeviceAvailabilityService
{
    public const string DoNotStartRecommendation = "DoNotStartMonitoring";

    public async Task<DeviceAvailability> CheckAsync(Guid operatorId)
    {
        var uri = "api/v1/devices/pre-shift-availability" +
                  $"?operatorId={operatorId}" +
                  $"&batteryThreshold={batteryThreshold.ToString(CultureInfo.InvariantCulture)}";

        try
        {
            using var response = await httpClient.GetAsync(uri);
            if (!response.IsSuccessStatusCode)
                return DeviceAvailability.Unavailable(
                    $"The Device Management service could not check the devices (HTTP {(int)response.StatusCode}).");

            var body = await response.Content.ReadFromJsonAsync<PreShiftAvailabilityResponse>();
            if (body is null)
                return DeviceAvailability.Unavailable("The Device Management service returned an empty response.");

            return body.Recommendation == DoNotStartRecommendation
                ? DeviceAvailability.Unavailable(
                    $"The operator's devices are not available to start monitoring ({body.Status}).")
                : DeviceAvailability.Available();
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException)
        {
            return DeviceAvailability.Unavailable(
                $"The Device Management service is not reachable: {exception.Message}");
        }
    }

    private sealed record PreShiftAvailabilityResponse(string Status, string Recommendation);
}
