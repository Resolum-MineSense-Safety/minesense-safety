using ReportingService.Domain.Services;

namespace ReportingService.Tests.Application;

public sealed class FakeReportRequestQueue : IReportRequestQueue
{
    public List<string> Messages { get; } = [];
    public Exception? Failure { get; set; }

    public Task SendAsync(string messageBody, CancellationToken cancellationToken)
    {
        if (Failure is not null) throw Failure;
        Messages.Add(messageBody);
        return Task.CompletedTask;
    }
}

public sealed class FakeReportObjectStore : IReportObjectStore
{
    public System.Collections.Concurrent.ConcurrentDictionary<string, byte[]> Objects { get; } = new();
    public Exception? Failure { get; set; }
    public int Reads { get; private set; }

    public Task<Stream?> OpenReadAsync(string key, CancellationToken cancellationToken)
    {
        Reads++;
        if (Failure is not null) throw Failure;
        return Task.FromResult<Stream?>(Objects.TryGetValue(key, out var content) ? new MemoryStream(content) : null);
    }
}

public sealed class ManualTimeProvider(DateTimeOffset now) : TimeProvider
{
    private DateTimeOffset current = now;

    public override DateTimeOffset GetUtcNow() => current;

    public void Advance(TimeSpan time) => current += time;
}
