using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;

namespace MineSenseSafety.Shared.Interfaces.ASP.Configuration;

/// <summary>
/// Adds the Kubernetes probes at the very start of every microservice pipeline, before
/// HTTPS redirection and authentication, so the kubelet and the load balancer can call them
/// without a token:
/// /health/live answers while the process runs (no dependency checks, so a slow database
/// never restarts the pod) and /health/ready runs every registered check before the pod
/// receives traffic.
/// </summary>
public class HealthProbesStartupFilter : IStartupFilter
{
    public const string LivenessPath = "/health/live";
    public const string ReadinessPath = "/health/ready";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.UseHealthChecks(LivenessPath, new HealthCheckOptions { Predicate = _ => false });
        app.UseHealthChecks(ReadinessPath);
        next(app);
    };
}
