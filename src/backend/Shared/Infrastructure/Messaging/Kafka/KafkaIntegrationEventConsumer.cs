using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace MineSenseSafety.Shared.Infrastructure.Messaging.Kafka;

/// <summary>
/// Consumer loop of a service: subscribes its consumer group to the topics of its handlers,
/// hands every message to the <see cref="IntegrationEventProcessor"/> (idempotency, retries,
/// poison skip) and commits the offset only after the message was handled or skipped.
/// </summary>
public sealed class KafkaIntegrationEventConsumer(
    IOptions<KafkaOptions> options,
    IServiceScopeFactory scopeFactory,
    IntegrationEventProcessor processor,
    ILogger<KafkaIntegrationEventConsumer> logger) : BackgroundService
{
    private static readonly TimeSpan ReconnectDelay = TimeSpan.FromSeconds(5);
    private const int TopicCreationAttempts = 3;

    protected override Task ExecuteAsync(CancellationToken stoppingToken) =>
        // Consume() blocks, so the loop runs on its own thread and never delays the host start.
        Task.Factory.StartNew(() => RunAsync(stoppingToken), stoppingToken,
            TaskCreationOptions.LongRunning, TaskScheduler.Default).Unwrap();

    private async Task RunAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        var topics = SubscribedTopics();
        if (topics.Count == 0) return;
        await EnsureTopicsAsync(settings, topics, stoppingToken);

        var config = new ConsumerConfig
        {
            GroupId = settings.ConsumerGroup ?? $"minesense-{settings.ServiceName}",
            EnableAutoCommit = false,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            AllowAutoCreateTopics = true
        };
        KafkaClientConfiguration.Apply(config, settings, $"{settings.ServiceName}-consumer");

        var builder = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) => logger.LogWarning("Kafka consumer error: {Reason}", error.Reason));
        var tokenRefresh = KafkaClientConfiguration.TokenRefreshHandler(settings);
        if (tokenRefresh is not null) builder.SetOAuthBearerTokenRefreshHandler(tokenRefresh);

        using var consumer = builder.Build();
        consumer.Subscribe(topics);
        logger.LogInformation("Consumer group {Group} subscribed to {Topics}", config.GroupId, string.Join(", ", topics));

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;
                try
                {
                    result = consumer.Consume(stoppingToken);
                }
                catch (ConsumeException exception)
                {
                    logger.LogWarning("Kafka consume failed: {Reason}", exception.Error.Reason);
                    await Task.Delay(ReconnectDelay, stoppingToken);
                    continue;
                }

                if (result?.Message is null) continue;
                await HandleAsync(result, stoppingToken);
                consumer.Commit(result);
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Normal shutdown: the last uncommitted message is redelivered on the next start.
        }
        finally
        {
            consumer.Close();
        }
    }

    private List<string> SubscribedTopics()
    {
        using var scope = scopeFactory.CreateScope();
        return scope.ServiceProvider.GetServices<IIntegrationEventHandler>()
            .Select(handler => handler.Topic).Distinct().ToList();
    }

    private async Task HandleAsync(ConsumeResult<string, string> result, CancellationToken stoppingToken)
    {
        var integrationEvent = IntegrationEvent.TryDeserialize(result.Message.Value);
        if (integrationEvent is null)
        {
            logger.LogError("Skipping a message that is not a MineSense event at {TopicPartitionOffset}",
                result.TopicPartitionOffset);
            return;
        }

        var outcome = await processor.ProcessAsync(result.Topic, integrationEvent, stoppingToken);
        logger.LogInformation("Event {EventId} ({Type}) from {Topic}: {Outcome}",
            integrationEvent.EventId, integrationEvent.Type, result.Topic, outcome);
    }

    /// <summary>Creates the subscribed topics that do not exist yet (brokers may forbid auto-creation).</summary>
    private async Task EnsureTopicsAsync(KafkaOptions settings, List<string> topics,
        CancellationToken stoppingToken)
    {
        var config = new AdminClientConfig();
        KafkaClientConfiguration.Apply(config, settings, $"{settings.ServiceName}-admin");
        var builder = new AdminClientBuilder(config);
        var tokenRefresh = KafkaClientConfiguration.TokenRefreshHandler(settings);
        if (tokenRefresh is not null) builder.SetOAuthBearerTokenRefreshHandler(tokenRefresh);

        for (var attempt = 1; attempt <= TopicCreationAttempts; attempt++)
        {
            try
            {
                using var admin = builder.Build();
                await admin.CreateTopicsAsync(topics.Select(topic => new TopicSpecification
                {
                    Name = topic,
                    NumPartitions = settings.TopicPartitions,
                    ReplicationFactor = -1
                }));
                return;
            }
            catch (CreateTopicsException exception) when (exception.Results.All(topic =>
                           topic.Error.Code is ErrorCode.NoError or ErrorCode.TopicAlreadyExists))
            {
                return;
            }
            catch (KafkaException exception)
            {
                // Without permission to create topics the consumer still subscribes to the existing ones.
                logger.LogWarning("Kafka topics not created ({Reason}), attempt {Attempt}/{Attempts}",
                    exception.Error.Reason, attempt, TopicCreationAttempts);
                if (attempt < TopicCreationAttempts) await Task.Delay(ReconnectDelay, stoppingToken);
            }
        }
    }
}
