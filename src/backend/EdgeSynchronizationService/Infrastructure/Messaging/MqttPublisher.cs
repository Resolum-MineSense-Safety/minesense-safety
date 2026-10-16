using System.Text;
using System.Text.Json;
using EdgeSynchronizationService.Domain.Model.Aggregates;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace EdgeSynchronizationService.Infrastructure.Messaging;

public class MqttPublisher(IOptions<MqttOptions> options, ILogger<MqttPublisher> logger) : IMqttPublisher
{
    public async Task PublishAsync(SyncEvent syncEvent)
    {
        var factory = new MqttClientFactory();
        using var client = factory.CreateMqttClient();
        await client.ConnectAsync(new MqttClientOptionsBuilder()
            .WithTcpServer(options.Value.Host, options.Value.Port).Build());

        var message = new MqttApplicationMessageBuilder()
            .WithTopic($"minesense/{syncEvent.VehicleId}/{syncEvent.EventType}")
            .WithPayload(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(syncEvent)))
            .WithAtLeastOnceQoS().Build();
        await client.PublishAsync(message);
        await client.DisconnectAsync();
        logger.LogInformation("Published synchronized event {EventId}", syncEvent.EventId);
    }
}
