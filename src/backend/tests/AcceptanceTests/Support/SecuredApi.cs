extern alias fatigue;
extern alias alerts;

using System.Collections.Concurrent;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;

namespace MineSenseSafety.AcceptanceTests.Support;

/// <summary>
/// Fatigue Detection and Alerts with token enforcement on, as in docker-compose and Kubernetes
/// (NFR02). Identity &amp; Access is unreachable on purpose, so no token can be valid; every log
/// line of both services is captured to check that rejected attempts are recorded.
/// </summary>
public sealed class SecuredApi : IDisposable
{
    private readonly WebApplicationFactory<fatigue::Program> _fatigueFactory;
    private readonly WebApplicationFactory<alerts::Program> _alertsFactory;

    public SecuredApi()
    {
        _fatigueFactory = new WebApplicationFactory<fatigue::Program>().WithWebHostBuilder(Secure);
        _alertsFactory = new WebApplicationFactory<alerts::Program>().WithWebHostBuilder(Secure);
    }

    public CapturingLoggerProvider Logs { get; } = new();

    public HttpClient Client(string service) => service switch
    {
        "Fatigue Detection" => _fatigueFactory.CreateClient(),
        "Alerts" => _alertsFactory.CreateClient(),
        _ => throw new ArgumentOutOfRangeException(nameof(service), service, "Unknown service")
    };

    private void Secure(IWebHostBuilder builder) => builder
        .UseSetting("Authentication:IdentityUrl", "http://identity.invalid")
        .ConfigureLogging(logging => logging.AddProvider(Logs));

    public void Dispose()
    {
        _fatigueFactory.Dispose();
        _alertsFactory.Dispose();
    }
}

/// <summary>Keeps every formatted log message in memory.</summary>
public sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<string> Messages { get; } = new();

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(Messages);

    public void Dispose()
    {
    }

    private sealed class CapturingLogger(ConcurrentQueue<string> messages) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) => messages.Enqueue(formatter(state, exception));
    }
}
