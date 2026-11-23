extern alias fatigue;
extern alias alerts;
extern alias identity;
extern alias operations;
extern alias devices;
extern alias monitoring;
extern alias fleet;
extern alias incidents;
extern alias analytics;
extern alias feedback;
extern alias privacy;
extern alias reporting;
extern alias edgesync;

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace MineSenseSafety.AcceptanceTests.Support;

/// <summary>Microservices of src/backend that the scenarios call.</summary>
public enum Service
{
    FatigueDetection,
    Alerts,
    IdentityAccess,
    Operations,
    DeviceManagement,
    Monitoring,
    FleetMonitoring,
    IncidentManagement,
    Analytics,
    FeedbackReliability,
    PrivacyAudit,
    Reporting,
    EdgeSynchronization
}

/// <summary>
/// Gives each step an <see cref="HttpClient"/> for a microservice. In memory, every service used by a
/// scenario is started on first use with WebApplicationFactory and thrown away at the end of the
/// scenario, so scenarios never share data. Against a deployed environment every client points to the
/// API gateway. Reqnroll creates one instance per scenario and disposes it afterwards.
/// </summary>
public sealed class MineSenseServices : IDisposable
{
    /// <summary>Bootstrap administrator of the in-memory Identity &amp; Access.</summary>
    public const string AdministratorUsername = "acceptance-admin";
    public const string AdministratorPassword = "Acceptance-Admin-2026";

    private readonly Dictionary<Service, InMemoryHost> _hosts = new();
    private readonly Dictionary<Service, InMemoryHost> _securedHosts = new();
    private readonly Dictionary<Service, FakeTimeProvider> _clocks = new();
    private readonly List<IDisposable> _disposables = [];

    /// <summary>Services whose clock is replaced in memory, so deadlines are tested without waiting.</summary>
    private static readonly Service[] ServicesWithFakeClock =
        [Service.Alerts, Service.IncidentManagement, Service.FleetMonitoring];

    public static bool IsRemote => AcceptanceSettings.IsRemote;

    /// <summary>Client for the service; against a deployed environment it carries the acceptance token.</summary>
    public HttpClient Client(Service service) =>
        IsRemote ? RemoteEnvironment.AuthorizedClientAsync().GetAwaiter().GetResult() : Host(service).Client;

    /// <summary>Client without credentials.</summary>
    public HttpClient Anonymous(Service service) => IsRemote ? RemoteEnvironment.AnonymousClient() : Host(service).Client;

    /// <summary>
    /// Client for the service with token validation switched on (<c>Authentication:IdentityUrl</c>
    /// set, as in Docker Compose and Helm) and no credentials. Deployed environments already run that way.
    /// </summary>
    public HttpClient SecuredAnonymous(Service service) =>
        IsRemote ? RemoteEnvironment.AnonymousClient() : SecuredHost(service).Client;

    /// <summary>Same as <see cref="SecuredAnonymous"/>, presenting the given bearer token.</summary>
    public HttpClient SecuredWithBearer(Service service, string token)
    {
        HttpClient client;
        if (IsRemote)
        {
            client = RemoteEnvironment.WithBearer(token);
        }
        else
        {
            client = SecuredHost(service).Factory.CreateClient();
            client.DefaultRequestHeaders.Authorization = new("Bearer", token);
        }

        _disposables.Add(client);
        return client;
    }

    /// <summary>Clock of a service hosted in memory (Alerts, Incident Management, Fleet Monitoring).</summary>
    public FakeTimeProvider Clock(Service service)
    {
        if (IsRemote)
            throw new InvalidOperationException("The clock of a deployed service cannot be moved; tag the scenario @solo-en-memoria.");
        Host(service);
        return _clocks.TryGetValue(service, out var clock)
            ? clock
            : throw new InvalidOperationException($"{service} runs with the system clock.");
    }

    private InMemoryHost Host(Service service)
    {
        if (!_hosts.TryGetValue(service, out var host))
        {
            host = Start(service, secured: false);
            _hosts[service] = host;
        }

        return host;
    }

    private InMemoryHost SecuredHost(Service service)
    {
        if (!_securedHosts.TryGetValue(service, out var host))
        {
            host = Start(service, secured: true);
            _securedHosts[service] = host;
        }

        return host;
    }

    private InMemoryHost Start(Service service, bool secured)
    {
        void Configure(IWebHostBuilder builder)
        {
            if (secured)
                // Nothing answers at this address: no token can be validated, as with a forged one.
                builder.UseSetting("Authentication:IdentityUrl", "http://identity.invalid");

            if (!secured && ServicesWithFakeClock.Contains(service))
            {
                var clock = new FakeTimeProvider(DateTimeOffset.UtcNow);
                _clocks[service] = clock;
                builder.ConfigureTestServices(services =>
                {
                    services.RemoveAll<TimeProvider>();
                    services.AddSingleton<TimeProvider>(clock);
                });
            }

            switch (service)
            {
                case Service.IdentityAccess:
                    builder.UseSetting("IdentityAccess:BootstrapAdministrator:Username", AdministratorUsername);
                    builder.UseSetting("IdentityAccess:BootstrapAdministrator:Password", AdministratorPassword);
                    break;
                case Service.Monitoring when !secured:
                    // Monitoring asks Device Management for the pre-shift availability over HTTP; in
                    // memory that call goes to the Device Management host of the same scenario.
                    var deviceManagement = Host(Service.DeviceManagement).Factory;
                    builder.ConfigureTestServices(services =>
                        services.AddHttpClient("IDeviceAvailabilityService")
                            .ConfigurePrimaryHttpMessageHandler(() => deviceManagement.Server.CreateHandler()));
                    break;
            }
        }

        WebApplicationFactory<TEntryPoint> Factory<TEntryPoint>() where TEntryPoint : class
        {
            var root = new WebApplicationFactory<TEntryPoint>();
            _disposables.Add(root);
            return root.WithWebHostBuilder(Configure);
        }

        IWebApplicationFactory factory = service switch
        {
            Service.FatigueDetection => new Wrapped<fatigue::Program>(Factory<fatigue::Program>()),
            Service.Alerts => new Wrapped<alerts::Program>(Factory<alerts::Program>()),
            Service.IdentityAccess => new Wrapped<identity::Program>(Factory<identity::Program>()),
            Service.Operations => new Wrapped<operations::Program>(Factory<operations::Program>()),
            Service.DeviceManagement => new Wrapped<devices::Program>(Factory<devices::Program>()),
            Service.Monitoring => new Wrapped<monitoring::Program>(Factory<monitoring::Program>()),
            Service.FleetMonitoring => new Wrapped<fleet::Program>(Factory<fleet::Program>()),
            Service.IncidentManagement => new Wrapped<incidents::Program>(Factory<incidents::Program>()),
            Service.Analytics => new Wrapped<analytics::Program>(Factory<analytics::Program>()),
            Service.FeedbackReliability => new Wrapped<feedback::Program>(Factory<feedback::Program>()),
            Service.PrivacyAudit => new Wrapped<privacy::Program>(Factory<privacy::Program>()),
            Service.Reporting => new Wrapped<reporting::Program>(Factory<reporting::Program>()),
            Service.EdgeSynchronization => new Wrapped<edgesync::Program>(Factory<edgesync::Program>()),
            _ => throw new ArgumentOutOfRangeException(nameof(service), service, null)
        };
        _disposables.Add(factory);
        var client = factory.CreateClient();
        _disposables.Add(client);
        return new InMemoryHost(factory, client);
    }

    public void Dispose()
    {
        // Clients first, then the derived factories, then the root factories.
        for (var index = _disposables.Count - 1; index >= 0; index--)
            _disposables[index].Dispose();
        _disposables.Clear();
    }

    private sealed record InMemoryHost(IWebApplicationFactory Factory, HttpClient Client);

    /// <summary>The generic factories of the services have no common base, so they are wrapped.</summary>
    private interface IWebApplicationFactory : IDisposable
    {
        TestServer Server { get; }

        HttpClient CreateClient();
    }

    private sealed class Wrapped<TEntryPoint>(WebApplicationFactory<TEntryPoint> factory) : IWebApplicationFactory
        where TEntryPoint : class
    {
        public TestServer Server => factory.Server;

        public HttpClient CreateClient() => factory.CreateClient();

        public void Dispose() => factory.Dispose();
    }
}
