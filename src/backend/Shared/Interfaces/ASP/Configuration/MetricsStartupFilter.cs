using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Prometheus;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// Exposes Prometheus metrics at <c>/metrics</c> in every microservice (section 4.3): request
/// count and duration per HTTP method and status code (<c>http_request_duration_seconds</c>),
/// requests in progress and the .NET runtime (GC, thread pool, memory). Added as a startup
/// filter, before HTTPS redirection and authentication, so Prometheus scrapes it without a
/// token; the ALB never routes <c>/metrics</c>, so it stays inside the cluster.
/// </summary>
public class MetricsStartupFilter : IStartupFilter
{
    public const string MetricsPath = "/metrics";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.UseMetricServer(MetricsPath);
        app.UseHttpMetrics();
        next(app);
    };
}
