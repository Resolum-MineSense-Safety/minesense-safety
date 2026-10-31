using MineSenseSafety.Shared.Domain.Model;

namespace MineSenseSafety.Shared.Tests.Domain.Model;

public enum SampleState
{
    Created,
    Recorded
}

public record SampleValue(double Ratio, string Shift);

/// <summary>Aggregate shaped like the real ones: private setters, a private list and a value object.</summary>
public class SampleAggregate : AggregateRoot
{
    private readonly List<string> _entries = [];

    public string Name { get; private set; }
    public SampleValue Value { get; private set; }
    public DateTimeOffset At { get; }
    public SampleState State { get; private set; }
    public IReadOnlyList<string> Entries => _entries;

    public SampleAggregate(string name, SampleValue value, DateTimeOffset at)
    {
        Name = name;
        Value = value;
        At = at;
    }

    public void Record(string entry)
    {
        _entries.Add(entry);
        State = SampleState.Recorded;
    }
}
