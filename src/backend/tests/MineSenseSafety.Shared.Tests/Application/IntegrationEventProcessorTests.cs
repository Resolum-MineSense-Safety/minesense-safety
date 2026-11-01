using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Infrastructure.Messaging;

namespace MineSenseSafety.Shared.Tests.Application;

public class IntegrationEventProcessorTests
{
    private const string Topic = "minesense.test.happened";

    /// <summary>Handler double that counts calls and can fail a number of times first.</summary>
    private sealed class CountingHandler : IIntegrationEventHandler
    {
        public int Calls { get; private set; }
        public int FailuresBeforeSuccess { get; init; }
        public Guid? SeenCorrelationId { get; private set; }
        public string Topic { get; init; } = IntegrationEventProcessorTests.Topic;

        public Task HandleAsync(IntegrationEvent integrationEvent, CancellationToken cancellationToken)
        {
            Calls++;
            SeenCorrelationId = IntegrationEventContext.CorrelationId;
            if (Calls <= FailuresBeforeSuccess) throw new InvalidOperationException("transient failure");
            return Task.CompletedTask;
        }
    }

    private static (IntegrationEventProcessor Processor, InMemoryProcessedEventStore Store) Create(
        params IIntegrationEventHandler[] handlers)
    {
        var services = new ServiceCollection();
        foreach (var handler in handlers) services.AddSingleton(handler);
        var store = new InMemoryProcessedEventStore();
        var processor = new IntegrationEventProcessor(services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>(),
            store, NullLogger<IntegrationEventProcessor>.Instance, maxAttempts: 3, retryDelay: TimeSpan.Zero);
        return (processor, store);
    }

    private static IntegrationEvent NewEvent() =>
        IntegrationEvent.Create("TestHappened", "test", Guid.NewGuid(), new { value = 1 }, DateTimeOffset.UtcNow);

    [Fact]
    public async Task ProcessAsync_NewEvent_RunsHandlersOfItsTopicOnly()
    {
        // Arrange
        var handler = new CountingHandler();
        var other = new CountingHandler { Topic = "minesense.other" };
        var (processor, store) = Create(handler, other);
        var integrationEvent = NewEvent();

        // Act
        var outcome = await processor.ProcessAsync(Topic, integrationEvent, CancellationToken.None);

        // Assert
        Assert.Equal(IntegrationEventOutcome.Handled, outcome);
        Assert.Equal(1, handler.Calls);
        Assert.Equal(0, other.Calls);
        Assert.True(store.HasProcessed(integrationEvent.EventId));
        Assert.Equal(integrationEvent.CorrelationId, handler.SeenCorrelationId);
    }

    [Fact]
    public async Task ProcessAsync_RedeliveredEvent_IsIgnored()
    {
        // Arrange
        var handler = new CountingHandler();
        var (processor, _) = Create(handler);
        var integrationEvent = NewEvent();
        await processor.ProcessAsync(Topic, integrationEvent, CancellationToken.None);

        // Act
        var outcome = await processor.ProcessAsync(Topic, integrationEvent, CancellationToken.None);

        // Assert
        Assert.Equal(IntegrationEventOutcome.Duplicate, outcome);
        Assert.Equal(1, handler.Calls);
    }

    [Fact]
    public async Task ProcessAsync_TransientFailure_IsRetriedUntilItSucceeds()
    {
        // Arrange
        var handler = new CountingHandler { FailuresBeforeSuccess = 2 };
        var (processor, _) = Create(handler);

        // Act
        var outcome = await processor.ProcessAsync(Topic, NewEvent(), CancellationToken.None);

        // Assert
        Assert.Equal(IntegrationEventOutcome.Handled, outcome);
        Assert.Equal(3, handler.Calls);
    }

    [Fact]
    public async Task ProcessAsync_PoisonEvent_IsSkippedAfterMaxAttemptsAndNotRetriedAgain()
    {
        // Arrange
        var handler = new CountingHandler { FailuresBeforeSuccess = int.MaxValue };
        var (processor, store) = Create(handler);
        var integrationEvent = NewEvent();

        // Act
        var outcome = await processor.ProcessAsync(Topic, integrationEvent, CancellationToken.None);
        var redelivery = await processor.ProcessAsync(Topic, integrationEvent, CancellationToken.None);

        // Assert
        Assert.Equal(IntegrationEventOutcome.Skipped, outcome);
        Assert.Equal(IntegrationEventOutcome.Duplicate, redelivery);
        Assert.Equal(3, handler.Calls);
        Assert.True(store.HasProcessed(integrationEvent.EventId));
    }

    [Fact]
    public async Task ProcessAsync_Cancelled_StopsWithoutMarkingTheEvent()
    {
        // Arrange
        var handler = new CountingHandler { FailuresBeforeSuccess = int.MaxValue };
        var store = new InMemoryProcessedEventStore();
        var services = new ServiceCollection().AddSingleton<IIntegrationEventHandler>(handler).BuildServiceProvider();
        var processor = new IntegrationEventProcessor(services.GetRequiredService<IServiceScopeFactory>(), store,
            NullLogger<IntegrationEventProcessor>.Instance, maxAttempts: 3, retryDelay: TimeSpan.FromSeconds(30));
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(100));
        var integrationEvent = NewEvent();

        // Act
        var action = () => processor.ProcessAsync(Topic, integrationEvent, cancellation.Token);

        // Assert
        await Assert.ThrowsAnyAsync<OperationCanceledException>(action);
        Assert.False(store.HasProcessed(integrationEvent.EventId));
    }
}
