using System.Text.Json;
using MineSenseSafety.Shared.Application.IntegrationEvents;

namespace MineSenseSafety.Shared.Tests.Application;

public class IntegrationEventTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 30, 8, 0, 0, TimeSpan.Zero);

    private record SamplePayload(Guid AlertId, string Severity, bool RequiresAlert);

    [Fact]
    public void SerializeAndDeserialize_KeepsEnvelopeAndPayload()
    {
        // Arrange
        var aggregateId = Guid.NewGuid();
        var payload = new SamplePayload(aggregateId, "Critical", true);
        var integrationEvent = IntegrationEvent.Create("AlertIssued", "alert-service", aggregateId, payload, Now);

        // Act
        var restored = IntegrationEvent.TryDeserialize(integrationEvent.Serialize());

        // Assert
        Assert.NotNull(restored);
        Assert.Equal(integrationEvent.EventId, restored.EventId);
        Assert.Equal("AlertIssued", restored.Type);
        Assert.Equal(Now, restored.OccurredAt);
        Assert.Equal(1, restored.Version);
        Assert.Equal("alert-service", restored.Source);
        Assert.Equal(aggregateId.ToString(), restored.AggregateId);
        Assert.Equal(payload, restored.GetPayload<SamplePayload>());
    }

    [Fact]
    public void Serialize_UsesCamelCaseFieldsForEveryConsumer()
    {
        // Arrange
        var integrationEvent = IntegrationEvent.Create("AlertIssued", "alert-service", Guid.NewGuid(),
            new SamplePayload(Guid.NewGuid(), "Warning", false), Now);

        // Act
        using var json = JsonDocument.Parse(integrationEvent.Serialize());

        // Assert
        var root = json.RootElement;
        foreach (var field in new[] { "eventId", "type", "occurredAt", "version", "source", "correlationId", "aggregateId" })
            Assert.True(root.TryGetProperty(field, out _), $"Missing {field}");
        Assert.Equal("Warning", root.GetProperty("payload").GetProperty("severity").GetString());
    }

    [Fact]
    public void Create_WithoutCorrelation_UsesTheEventIdAndInsideAHandlerInheritsIt()
    {
        // Arrange
        var correlationId = Guid.NewGuid();

        // Act
        var standalone = IntegrationEvent.Create("A", "test", Guid.NewGuid(), new { }, Now);
        IntegrationEvent inherited;
        using (IntegrationEventContext.Begin(correlationId))
            inherited = IntegrationEvent.Create("B", "test", Guid.NewGuid(), new { }, Now);

        // Assert
        Assert.Equal(standalone.EventId, standalone.CorrelationId);
        Assert.Equal(correlationId, inherited.CorrelationId);
        Assert.Null(IntegrationEventContext.CorrelationId);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("{\"type\":\"AlertIssued\"}")]
    public void TryDeserialize_InvalidMessage_ReturnsNull(string? message)
    {
        // Act
        var integrationEvent = IntegrationEvent.TryDeserialize(message);

        // Assert
        Assert.Null(integrationEvent);
    }
}
