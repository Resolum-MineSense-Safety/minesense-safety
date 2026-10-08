namespace MineSenseSafety.Shared.Domain.Model;

/// <summary>
/// Base type for every aggregate root of the bounded contexts.
/// Identity is a Guid so aggregates can be created on the Edge and synchronized later.
/// </summary>
public abstract class AggregateRoot
{
    public Guid Id { get; protected init; } = Guid.NewGuid();
}
