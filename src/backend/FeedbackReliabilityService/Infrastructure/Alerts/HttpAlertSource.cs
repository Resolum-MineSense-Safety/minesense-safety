using System.Net;
using System.Text.Json;
using FeedbackReliabilityService.Application.Internal;
using FeedbackReliabilityService.Application.Internal.OutboundServices;

namespace FeedbackReliabilityService.Infrastructure.Alerts;

public class HttpAlertSource(HttpClient client) : IAlertSource
{
    public async Task<SourceAlert?> FindAsync(Guid alertId, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync($"api/v1/alerts/{alertId}", cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound) return null;
            if (!response.IsSuccessStatusCode) throw new AlertSourceUnavailableException();
            var alert = await response.Content.ReadFromJsonAsync<SourceAlert>(cancellationToken);
            if (alert is null || alert.Id != alertId || alert.OperatorId == Guid.Empty || alert.IssuedAt == default)
                throw new AlertSourceUnavailableException();
            return alert;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException ||
                                        (exception is OperationCanceledException && !cancellationToken.IsCancellationRequested))
        {
            throw new AlertSourceUnavailableException();
        }
    }
}
