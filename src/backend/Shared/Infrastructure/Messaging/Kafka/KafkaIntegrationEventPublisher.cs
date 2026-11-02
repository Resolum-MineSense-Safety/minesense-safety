using System.Text;
using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace MineSenseSafety.Shared.Infrastructure.Messaging.Kafka;

/// <summary>
/// Idempotent Kafka producer (acks=all, enable.idempotence) keyed by aggregate id, so the events of
/// one aggregate keep their order in a partition. Sending is asynchronous: the REST answer of a
/// use case never waits for the broker and delivery failures are logged.
/// </summary>
public sealed class KafkaIntegrationEventPublisher : IIntegrationEventPublisher, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaIntegrationEventPublisher> _logger;

    public KafkaIntegrationEventPublisher(IOptions<KafkaOptions> options, ILogger<KafkaIntegrationEventPublisher> logger)
    {
        _logger = logger;
        var config = new ProducerConfig
        {
            EnableIdempotence = true,
            Acks = Acks.All,
            LingerMs = 5,
            MessageTimeoutMs = 30_000
        };
        KafkaClientConfiguration.Apply(config, options.Value, $"{options.Value.ServiceName}-producer");

        var builder = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => _logger.LogWarning("Kafka producer error: {Reason}", error.Reason));
        var tokenRefresh = KafkaClientConfiguration.TokenRefreshHandler(options.Value);
        if (tokenRefresh is not null) builder.SetOAuthBearerTokenRefreshHandler(tokenRefresh);
        _producer = builder.Build();
    }

    public Task PublishAsync(string topic, IntegrationEvent integrationEvent, string? partitionKey = null)
    {
        var message = new Message<string, string>
        {
            Key = partitionKey ?? integrationEvent.AggregateId,
            Value = integrationEvent.Serialize(),
            Headers = new Headers
            {
                { "event-type", Encoding.UTF8.GetBytes(integrationEvent.Type) },
                { "event-id", Encoding.UTF8.GetBytes(integrationEvent.EventId.ToString()) }
            }
        };

        try
        {
            _producer.Produce(topic, message, report =>
            {
                if (report.Error.IsError)
                    _logger.LogError("Event {EventId} ({Type}) was not delivered to {Topic}: {Reason}",
                        integrationEvent.EventId, integrationEvent.Type, topic, report.Error.Reason);
                else
                    _logger.LogInformation("Event {EventId} ({Type}) delivered to {TopicPartitionOffset}",
                        integrationEvent.EventId, integrationEvent.Type, report.TopicPartitionOffset);
            });
        }
        catch (Exception exception) when (exception is ProduceException<string, string> or KafkaException)
        {
            _logger.LogError(exception, "Event {EventId} ({Type}) could not be queued for {Topic}",
                integrationEvent.EventId, integrationEvent.Type, topic);
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _producer.Flush(TimeSpan.FromSeconds(5));
        _producer.Dispose();
    }
}
