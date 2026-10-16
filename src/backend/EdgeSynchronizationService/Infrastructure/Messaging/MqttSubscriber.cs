using System.Text;
using System.Text.Json;
using EdgeSynchronizationService.Domain.Model.Commands;
using EdgeSynchronizationService.Domain.Services;
using Microsoft.Extensions.Options;
using MQTTnet;

namespace EdgeSynchronizationService.Infrastructure.Messaging;

public class MqttSubscriber(
    IServiceScopeFactory scopeFactory,
    IOptions<MqttOptions> options,
    ILogger<MqttSubscriber> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var factory = new MqttClientFactory();
                using var client = factory.CreateMqttClient();
                client.ApplicationMessageReceivedAsync += message => HandleAsync(message, stoppingToken);
                await client.ConnectAsync(new MqttClientOptionsBuilder()
                    .WithTcpServer(options.Value.Host, options.Value.Port).Build(), stoppingToken);
                await client.SubscribeAsync(new MqttTopicFilterBuilder()
                    .WithTopic(options.Value.Subscription).Build(), stoppingToken);
                await Task.Delay(Timeout.InfiniteTimeSpan, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                logger.LogWarning(exception, "MQTT subscriber unavailable; retrying in 5 seconds");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
    }

    private async Task HandleAsync(MqttApplicationMessageReceivedEventArgs message, CancellationToken cancellationToken)
    {
        try
        {
            var command = JsonSerializer.Deserialize<EnqueueSyncEventCommand>(
                Encoding.UTF8.GetString(message.ApplicationMessage.PayloadSegment.ToArray()));
            if (command is null) throw new JsonException("The MQTT payload is empty.");

            using var scope = scopeFactory.CreateScope();
            await scope.ServiceProvider.GetRequiredService<ISynchronizationCommandService>()
                .EnqueueAsync(command);
        }
        catch (Exception exception) when (exception is JsonException or MineSenseSafety.Shared.Domain.Model.DomainException)
        {
            logger.LogWarning(exception, "Rejected MQTT message from {Topic}", message.ApplicationMessage.Topic);
        }
    }
}
