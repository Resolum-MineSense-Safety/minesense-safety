using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MineSenseSafety.Shared.Application.IntegrationEvents;
using MineSenseSafety.Shared.Infrastructure.Messaging;
using MineSenseSafety.Shared.Infrastructure.Messaging.Kafka;
using MineSenseSafety.Shared.Interfaces.ASP.Configuration;

namespace MineSenseSafety.Shared.Tests.Interfaces;

public class MineSenseEventsExtensionsTests
{
    private static IServiceCollection Register(string? bootstrapServers)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Kafka:BootstrapServers"] = bootstrapServers })
            .Build();
        return new ServiceCollection().AddLogging().AddMineSenseEvents(configuration, "alert-service");
    }

    [Fact]
    public void AddMineSenseEvents_WithoutBootstrapServers_LogsEventsAndStartsNoConsumer()
    {
        // Act
        var services = Register(null);

        // Assert
        using var provider = services.BuildServiceProvider();
        Assert.IsType<LoggingIntegrationEventPublisher>(provider.GetRequiredService<IIntegrationEventPublisher>());
        Assert.DoesNotContain(services, descriptor => descriptor.ImplementationType == typeof(KafkaIntegrationEventConsumer));
    }

    [Fact]
    public void AddMineSenseEvents_WithBootstrapServers_UsesKafkaAndTheServiceConsumerGroup()
    {
        // Act
        var services = Register("localhost:19092");

        // Assert
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IIntegrationEventPublisher)
                                                && descriptor.ImplementationType == typeof(KafkaIntegrationEventPublisher));
        Assert.Contains(services, descriptor => descriptor.ServiceType == typeof(IHostedService)
                                                && descriptor.ImplementationType == typeof(KafkaIntegrationEventConsumer));
        using var provider = services.BuildServiceProvider();
        Assert.Equal("alert-service", provider.GetRequiredService<IOptions<KafkaOptions>>().Value.ServiceName);
    }
}
