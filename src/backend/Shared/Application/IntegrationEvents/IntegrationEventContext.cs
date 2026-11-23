namespace MineSenseSafety.Shared.Application.IntegrationEvents;

/// <summary>
/// Correlation id of the event being handled in the current async flow. Events published while
/// handling another one inherit it, so the whole detection chain shares one correlation id.
/// </summary>
public static class IntegrationEventContext
{
    private static readonly AsyncLocal<Guid?> Current = new();

    public static Guid? CorrelationId => Current.Value;

    public static IDisposable Begin(Guid correlationId)
    {
        var previous = Current.Value;
        Current.Value = correlationId;
        return new Scope(() => Current.Value = previous);
    }

    private sealed class Scope(Action restore) : IDisposable
    {
        public void Dispose() => restore();
    }
}
